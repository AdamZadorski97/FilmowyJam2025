// SphereGenerator.cs
using UnityEngine;
using System.Collections.Generic; // Potrzebne do List<Vector3> i List<int>

public class SphereGenerator : MonoBehaviour
{
    // Ustawienia sfery
    public float radius = 10f; // Promień sfery
    public int subdivisions = 40; // Liczba podziałów - im więcej, tym gładsza sfera, ale bardziej wymagająca dla CPU

    // Wewnętrzne referencje do komponentów
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh mesh; // Faktyczna siatka 3D sfery

    // Metoda wywoływana, gdy skrypt się budzi (przed Start())
    void Awake()
    {
        // Sprawdź i dodaj MeshFilter, jeśli go nie ma
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        // Sprawdź i dodaj MeshRenderer, jeśli go nie ma
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            // Ustaw domyślny materiał, aby sfera była widoczna
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }

        // Sprawdź i dodaj MeshCollider, jeśli go nie ma
        // MeshCollider jest kluczowy do wykrywania kliknięć/kolizji z siatką
        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        // Upewnij się, że Convex jest wyłączony dla edycji terenu (chyba że potrzebujesz sztywnych brył)
        meshCollider.convex = false;

        // Wygeneruj początkową siatkę sfery
        GenerateSphere();
    }

    // Metoda do generowania siatki sfery
    void GenerateSphere()
    {
        mesh = new Mesh(); // Stwórz nową, pustą siatkę
        meshFilter.mesh = mesh; // Przypisz ją do MeshFilter

        List<Vector3> vertices = new List<Vector3>(); // Lista do przechowywania wierzchołków
        List<int> triangles = new List<int>(); // Lista do przechowywania indeksów trójkątów

        // Generowanie wierzchołków sfery (przy użyciu współrzędnych sferycznych)
        // Im większa 'subdivisions', tym więcej wierzchołków i trójkątów
        for (int i = 0; i <= subdivisions; i++)
        {
            float phi = Mathf.PI * i / subdivisions; // Kąt pionowy (od 0 do PI)
            for (int j = 0; j <= subdivisions; j++)
            {
                float theta = 2 * Mathf.PI * j / subdivisions; // Kąt poziomy (od 0 do 2*PI)

                float x = radius * Mathf.Sin(phi) * Mathf.Cos(theta);
                float y = radius * Mathf.Cos(phi);
                float z = radius * Mathf.Sin(phi) * Mathf.Sin(theta);

                vertices.Add(new Vector3(x, y, z));
            }
        }

        // Generowanie trójkątów, które łączą wierzchołki
        for (int i = 0; i < subdivisions; i++)
        {
            for (int j = 0; j < subdivisions; j++)
            {
                // Oblicz indeksy wierzchołków dla dwóch trójkątów tworzących kwadrat
                int current = i * (subdivisions + 1) + j;
                int nextRow = current + (subdivisions + 1);

                // Pierwszy trójkąt (górny lewy)
                triangles.Add(current);
                triangles.Add(nextRow);
                triangles.Add(current + 1);

                // Drugi trójkąt (dolny prawy)
                triangles.Add(nextRow);
                triangles.Add(nextRow + 1);
                triangles.Add(current + 1);
            }
        }

        // Przypisz wierzchołki i trójkąty do siatki
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        // Przelicz normalne (do oświetlenia) i granice (do renderowania/cullingu)
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // !!! KLUCZOWY KROK DLA MESH COLLIDERA !!!
        // Musimy przypisać siatkę do MeshColliderta, aby kolizje działały
        meshCollider.sharedMesh = mesh;
    }

    // Metoda do deformowania siatki sfery
    // `hitPoint`: punkt na siatce, który ma być "wypchnięty"
    // `deformationRadius`: promień wpływu deformacji wokół `hitPoint`
    // `deformationStrength`: siła, z jaką wierzchołki są wypychane
    public void DeformSphere(Vector3 hitPoint, float deformationRadius, float deformationStrength)
    {
        Vector3[] vertices = mesh.vertices; // Pobierz aktualne wierzchołki
        Vector3 sphereCenter = transform.position; // Środek sfery w przestrzeni globalnej

        for (int i = 0; i < vertices.Length; i++)
        {
            // Przelicz wierzchołek z przestrzeni lokalnej siatki do przestrzeni globalnej
            Vector3 worldVertex = transform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(worldVertex, hitPoint); // Odległość wierzchołka od punktu deformacji

            // Jeśli wierzchołek jest w zasięgu deformacji
            if (distance < deformationRadius)
            {
                // Oblicz kierunek od środka sfery do tego wierzchołka
                // To sprawi, że deformacja będzie "wypychać" wierzchołki na zewnątrz sfery
                Vector3 directionToVertex = (worldVertex - sphereCenter).normalized;

                // Obliczanie "spadku" siły deformacji wraz z odległością od `hitPoint`
                // Im bliżej `hitPoint`, tym silniejsza deformacja
                float falloff = Mathf.Pow(1 - (distance / deformationRadius), 2); // Używamy potęgi dla płynniejszego efektu

                // Oblicz wektor deformacji
                Vector3 deformationVector = directionToVertex * deformationStrength * falloff;

                // Zastosuj deformację do wierzchołka
                vertices[i] += deformationVector;
            }
        }
        mesh.vertices = vertices; // Przypisz zmodyfikowane wierzchołki z powrotem do siatki

        // Po zmianie wierzchołków, musimy ponownie przeliczyć normalne i granice
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // !!! KLUCZOWY KROK DLA MESH COLLIDERA PO MODYFIKACJI !!!
        // Musimy ponownie przypisać siatkę do MeshColliderta, aby odświeżyć jego kształt kolizji
        // Bez tego, kolider pozostanie w pierwotnym kształcie!
        meshCollider.sharedMesh = mesh;
    }
}