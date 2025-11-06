using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Do LINQ

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ProceduralSphere : MonoBehaviour
{
    public float initialRadius = 1f; // Początkowy promień sfery
    [Range(8, 64)] public int latitudeSegments = 32; // Początkowa ilość segmentów wzdłuż szerokości
    [Range(8, 64)] public int longitudeSegments = 32; // Początkowa ilość segmentów wzdłuż długości

    public float revealRadius = 5f;     // Promień, w którym gracz "odkrywa" (aktywnie modyfikuje) sferę
    public float expansionStrength = 0.5f; // Jak mocno nowe wierzchołki są wypychane
    public float maxEdgeLength = 0.2f;   // Maksymalna długość krawędzi przed podziałem (w lokalnych jednostkach)

    private Mesh mesh;
    private List<Vector3> currentVertices;
    private List<Vector2> currentUvs;
    private List<int> currentTriangles;

    // Struktura do śledzenia krawędzi i mapowania na nowe wierzchołki
    // Używamy Unity.Vector2 jako klucza dla pary indeksów wierzchołków krawędzi (posortowanych)
    private Dictionary<Vector2, int> edgeToNewVertexMap = new Dictionary<Vector2, int>();

    void Awake()
    {
        GenerateInitialSphere();
    }

    void GenerateInitialSphere()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer.sharedMaterial == null)
        {
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
        }

        mesh = new Mesh();
        meshFilter.mesh = mesh;

        currentVertices = new List<Vector3>();
        currentUvs = new List<Vector2>();
        currentTriangles = new List<int>();

        // Generowanie początkowych wierzchołków i UV (jak poprzednio)
        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float theta = lat * Mathf.PI / latitudeSegments;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float phi = lon * 2 * Mathf.PI / longitudeSegments;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                float x = initialRadius * cosPhi * sinTheta;
                float y = initialRadius * cosTheta;
                float z = initialRadius * sinPhi * sinTheta;

                currentVertices.Add(new Vector3(x, y, z));
                currentUvs.Add(new Vector2((float)lon / longitudeSegments, (float)lat / latitudeSegments));
            }
        }

        // Generowanie początkowych trójkątów (jak poprzednio)
        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int firstRow = lat * (longitudeSegments + 1);
                int secondRow = (lat + 1) * (longitudeSegments + 1);

                int v1 = firstRow + lon;
                int v2 = firstRow + lon + 1;
                int v3 = secondRow + lon + 1;
                int v4 = secondRow + lon;

                currentTriangles.Add(v1);
                currentTriangles.Add(v3);
                currentTriangles.Add(v2);

                currentTriangles.Add(v1);
                currentTriangles.Add(v4);
                currentTriangles.Add(v3);
            }
        }

        UpdateMesh();
    }

    // Aktualizuje siatkę Unity na podstawie List<>
    void UpdateMesh()
    {
        mesh.Clear(); // Wyczyść poprzednią siatkę
        mesh.vertices = currentVertices.ToArray();
        mesh.triangles = currentTriangles.ToArray();
        mesh.uv = currentUvs.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    /// <summary>
    /// Odkrywa i modyfikuje obszar sfery na podstawie pozycji gracza, potencjalnie dzieląc trójkąty.
    /// </summary>
    /// <param name="playerWorldPosition">Pozycja gracza w przestrzeni świata.</param>
    public void RevealArea(Vector3 playerWorldPosition)
    {
        Vector3 playerLocalPosition = transform.InverseTransformPoint(playerWorldPosition);

        // Tymczasowa lista nowych trójkątów, które zostaną dodane do siatki
        List<int> newTriangles = new List<int>();
        bool meshChanged = false;

        // Lista indeksów trójkątów, które zostaną usunięte (podzielone)
        HashSet<int> trianglesToRemove = new HashSet<int>();

        // Przesuń istniejące wierzchołki, jeśli są blisko gracza
        for (int i = 0; i < currentVertices.Count; i++)
        {
            float dist = Vector3.Distance(currentVertices[i], playerLocalPosition);
            if (dist < revealRadius)
            {
                // Oblicz kierunek od centrum sfery do wierzchołka
                Vector3 directionFromCenter = currentVertices[i].normalized;

                // Oblicz nową, powiększoną pozycję wierzchołka
                // Im bliżej, tym mocniej wypychaj. Normalizacja do promienia.
                float targetRadius = initialRadius + (revealRadius - dist) * expansionStrength;
                Vector3 targetPosition = directionFromCenter * targetRadius;

                // Płynna interpolacja
                currentVertices[i] = Vector3.Lerp(currentVertices[i], targetPosition, Time.deltaTime * 5f);
                meshChanged = true;
            }
        }

        // Iteruj po wszystkich TRÓJKĄTACH i sprawdzaj, czy należy je podzielić
        for (int i = 0; i < currentTriangles.Count; i += 3)
        {
            int v0_idx = currentTriangles[i];
            int v1_idx = currentTriangles[i + 1];
            int v2_idx = currentTriangles[i + 2];

            Vector3 v0 = currentVertices[v0_idx];
            Vector3 v1 = currentVertices[v1_idx];
            Vector3 v2 = currentVertices[v2_idx];

            // Sprawdź odległość środka trójkąta od gracza
            Vector3 triangleCenter = (v0 + v1 + v2) / 3f;
            if (Vector3.Distance(triangleCenter, playerLocalPosition) < revealRadius)
            {
                // Sprawdź długości krawędzi
                float d01 = Vector3.Distance(v0, v1);
                float d12 = Vector3.Distance(v1, v2);
                float d20 = Vector3.Distance(v2, v0);

                // Jeśli któraś krawędź jest za długa, podziel ten trójkąt
                if (d01 > maxEdgeLength || d12 > maxEdgeLength || d20 > maxEdgeLength)
                {
                    trianglesToRemove.Add(i); // Oznacz ten trójkąt do usunięcia
                    meshChanged = true;

                    // === Logika podziału trójkąta ===
                    // Podziel trójkąt na mniejsze, dodając wierzchołek na środku najdłuższej krawędzi.
                    // To jest najprostszy schemat tessellacji.

                    // Utwórz mapowanie krawędzi na indeksy wierzchołków dla łatwiejszego dodawania
                    // i unikania duplikatów wierzchołków na wspólnych krawędziach.
                    List<int> currentTriangleIndices = new List<int> { v0_idx, v1_idx, v2_idx };

                    // Podziel krawędzie trójkąta. Jeśli krawędź już została podzielona, użyj istniejącego wierzchołka.
                    // Jeśli nie, dodaj nowy wierzchołek.
                    int v01_mid_idx = GetOrCreateMidpointVertex(v0_idx, v1_idx);
                    int v12_mid_idx = GetOrCreateMidpointVertex(v1_idx, v2_idx);
                    int v20_mid_idx = GetOrCreateMidpointVertex(v2_idx, v0_idx);

                    // Utwórz 4 nowe trójkąty z podzielonego trójkąta
                    newTriangles.Add(v0_idx); newTriangles.Add(v01_mid_idx); newTriangles.Add(v20_mid_idx);
                    newTriangles.Add(v01_mid_idx); newTriangles.Add(v1_idx); newTriangles.Add(v12_mid_idx);
                    newTriangles.Add(v20_mid_idx); newTriangles.Add(v12_mid_idx); newTriangles.Add(v2_idx);
                    newTriangles.Add(v01_mid_idx); newTriangles.Add(v12_mid_idx); newTriangles.Add(v20_mid_idx);
                }
            }
        }

        // Usuń stare, podzielone trójkąty i dodaj nowe
        if (trianglesToRemove.Count > 0)
        {
            List<int> tempTriangles = new List<int>();
            for (int i = 0; i < currentTriangles.Count; i += 3)
            {
                if (!trianglesToRemove.Contains(i))
                {
                    tempTriangles.Add(currentTriangles[i]);
                    tempTriangles.Add(currentTriangles[i + 1]);
                    tempTriangles.Add(currentTriangles[i + 2]);
                }
            }
            currentTriangles = tempTriangles;
        }

        // Dodaj wszystkie nowo utworzone trójkąty
        currentTriangles.AddRange(newTriangles);

        if (meshChanged)
        {
            UpdateMesh();
        }
    }

    // Pomocnicza funkcja do uzyskiwania lub tworzenia wierzchołka w środku krawędzi
    private int GetOrCreateMidpointVertex(int idx1, int idx2)
    {
        // Posortuj indeksy, aby krawędź (1,2) była traktowana tak samo jak (2,1)
        Vector2 edgeKey = new Vector2(Mathf.Min(idx1, idx2), Mathf.Max(idx1, idx2));

        if (edgeToNewVertexMap.TryGetValue(edgeKey, out int newVertexIndex))
        {
            return newVertexIndex; // Krawędź już została podzielona, zwróć istniejący wierzchołek
        }
        else
        {
            // Krawędź nie została jeszcze podzielona, utwórz nowy wierzchołek
            Vector3 v1 = currentVertices[idx1];
            Vector3 v2 = currentVertices[idx2];
            Vector3 newPos = (v1 + v2) / 2f; // Środek krawędzi

            // Wypchnij nowy wierzchołek na promień sfery (initialRadius)
            newPos = newPos.normalized * initialRadius;

            currentVertices.Add(newPos);
            // Generuj UV dla nowego wierzchołka jako średnią z UV wierzchołków krawędzi
            currentUvs.Add((currentUvs[idx1] + currentUvs[idx2]) / 2f);

            int newIdx = currentVertices.Count - 1;
            edgeToNewVertexMap[edgeKey] = newIdx; // Zapisz mapowanie
            return newIdx;
        }
    }
}