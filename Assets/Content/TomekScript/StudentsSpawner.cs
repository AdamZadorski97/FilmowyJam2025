using UnityEngine;
using UnityEngine.AI;

public class StudentsSpawner : MonoBehaviour
{
    // Prefaby studentów, które mają być spawn'owane
    public GameObject[] Students;

    public GameObject[] StudentsInClass;

    // Ilość studentów do zespawnowania
    public int Count = 10;

    // Promień wyszukiwania punktu na NavMeshu
    public float SpawnRadius = 10f;



    /// <summary>
    /// Spawnuje określoną liczbę studentów w losowych, ważnych miejscach na NavMesh.
    /// </summary>
    // Stała określająca maksymalną liczbę prób dla pojedynczego spawnu
    private const int MAX_SPAWN_ATTEMPTS = 10;

    // Start jest wywoływany raz na początku
    void Start()
    {
        // Sprawdzamy, czy w ogóle mamy studentów do spawn'owania
        if (Students == null || Students.Length == 0)
        {
            Debug.LogError("Brak prefabów studentów w tablicy Students!");
            return;
        }

        SpawnStudents();
        EdypInClass();
    }

    private void EdypInClass()
    {
        foreach (var student in StudentsInClass)
        {
            student.GetComponent<Animator>().SetTrigger("Edyp");
        }
    }


    // Zmodyfikowana metoda SpawnStudents
    private void SpawnStudents()
    {
        int studentsSpawned = 0;

        // Pętla do momentu osiągnięcia żądanej liczby studentów
        for (int i = 0; i < Count; i++)
        {
            Vector3 randomPoint;
            bool spawnSuccessful = false;

            // Pętla prób (do 10 razy dla każdego studenta)
            for (int attempt = 0; attempt < MAX_SPAWN_ATTEMPTS; attempt++)
            {
                if (FindRandomPointOnNavMesh(out randomPoint))
                {
                    // Sukces! Znaleziono punkt

                    // 2. Wybierz losowy prefab studenta
                    GameObject randomStudentPrefab = Students[Random.Range(0, Students.Length)];

                    // 3. Zespawnuj studenta w znalezionym punkcie
                    Instantiate(randomStudentPrefab, randomPoint, Quaternion.identity);
                    studentsSpawned++;
                    spawnSuccessful = true;

                    // Przerwij pętlę prób, przejdź do następnego studenta
                    break;
                }
            }

            if (!spawnSuccessful)
            {
                Debug.LogWarning("Nie udało się znaleźć ważnego punktu na NavMesh dla studenta " + (i + 1) +
                                 " po " + MAX_SPAWN_ATTEMPTS + " próbach. Sprawdź 'SpawnRadius' i NavMesh.");
            }
        }

        Debug.Log($"Zakończono spawn: Zespawnowano {studentsSpawned} z {Count} studentów.");
    }

    /// <summary>
    /// Szuka losowego punktu na NavMesh w określonym promieniu.
    /// </summary>
    /// <param name="result">Zwraca znalezioną pozycję.</param>
    /// <returns>True, jeśli punkt został znaleziony.</returns>
    private bool FindRandomPointOnNavMesh(out Vector3 result)
    {
        // Generujemy losowy kierunek i odległość w promieniu SpawnRadius
        Vector3 randomDirection = Random.insideUnitSphere * SpawnRadius;
        randomDirection += transform.position; // Dodajemy do pozycji spawner'a

        NavMeshHit hit;

        // Szukamy najbliższego punktu na NavMesh w promieniu 1.0f (mała tolerancja)
        if (NavMesh.SamplePosition(randomDirection, out hit, 1.0f, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    // Update jest pusty, ponieważ spawn odbywa się tylko raz na początku gry.
    void Update()
    {

    }
}