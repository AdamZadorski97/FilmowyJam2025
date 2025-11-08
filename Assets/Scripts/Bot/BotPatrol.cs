using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // Ważne dla NavMeshAgent

// NOWA KLASA: Definiuje właściwości każdego punktu patrolu.
// [System.Serializable] sprawia, że możemy edytować ją w Inspektorze.
[System.Serializable]
public class PatrolWaypoint
{
    [Tooltip("Transform punktu, do którego bot pójdzie.")]
    public Transform point;

    [Tooltip("Czas (w sekundach), jaki bot spędzi na tym punkcie.")]
    public float waitTime = 0f;

    [Tooltip("Kierunek (w stopniach, kąty Eulera), w którym bot ma patrzeć PODCZAS postoju.")]
    public Vector3 targetRotation;
}

/// <summary>
/// Kontroluje bota AI, który patroluje listę punktów (waypoints)
/// w określonej kolejności, w pętli.
/// Potrafi czekać na punkcie i obracać się w określonym kierunku.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BotPatrol : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField]
    private NavMeshAgent agent;

    [Header("Ustawienia Patrolu")]
    [SerializeField]
    [Tooltip("Lista punktów patrolu (z czasem i rotacją). Kolejność ma znaczenie!")]
    private List<PatrolWaypoint> waypoints;

    [SerializeField]
    [Tooltip("Jak blisko bot musi podejść do punktu, aby uznać go za 'osiągnięty'.")]
    private float arrivalThreshold = 0.5f;

    [SerializeField]
    [Tooltip("Jak szybko bot obraca się w docelowy kierunek podczas postoju.")]
    private float rotationSpeed = 5f;

    // Zmienne wewnętrzne
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private BotState currentState;

    private enum BotState
    {
        Moving,
        Waiting
    }

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }

    private void Start()
    {
        if (agent == null)
        {
            Debug.LogError($"[BotPatrol] Na {gameObject.name} brakuje NavMeshAgent!", this);
            enabled = false;
            return;
        }

        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogError($"[BotPatrol] Na {gameObject.name} nie przypisano żadnych punktów!", this);
            enabled = false;
            return;
        }

        // Rozpocznij patrol
        StartMovingToNextWaypoint();
    }

    private void Update()
    {
        if (waypoints.Count == 0) return;

        // Prosta maszyna stanów (Moving / Waiting)
        switch (currentState)
        {
            case BotState.Moving:
                HandleMovingState();
                break;
            case BotState.Waiting:
                HandleWaitingState();
                break;
        }
    }

    /// <summary>
    /// Logika dla stanu, gdy bot jest W RUCHU.
    /// Sprawdza, czy dotarł do celu.
    /// </summary>
    private void HandleMovingState()
    {
        // Sprawdź, czy dotarliśmy do celu
        if (!agent.pathPending && agent.remainingDistance <= arrivalThreshold)
        {
            // Dotarliśmy. Przejdź do stanu oczekiwania.
            StartWaiting();
        }
    }

    /// <summary>
    /// Logika dla stanu, gdy bot OCZEKUJE na punkcie.
    /// Obraca się i odlicza czas.
    /// </summary>
    private void HandleWaitingState()
    {
        // 1. Obróć bota w kierunku zdefiniowanym w punkcie
        PatrolWaypoint currentWaypoint = waypoints[currentWaypointIndex];
        Quaternion targetLookRotation = Quaternion.Euler(currentWaypoint.targetRotation);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetLookRotation,
            rotationSpeed * Time.deltaTime
        );

        // 2. Odliczaj czas
        waitTimer -= Time.deltaTime;

        // 3. Jeśli czas minął, ruszaj do następnego punktu
        if (waitTimer <= 0f)
        {
            // Znajdź następny indeks, zapętlając listę
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            StartMovingToNextWaypoint();
        }
    }

    /// <summary>
    /// Przełącza bota w stan OCZEKIWANIA.
    /// </summary>
    private void StartWaiting()
    {
        currentState = BotState.Waiting;

        // Pobierz czas oczekiwania z aktualnego punktu
        waitTimer = waypoints[currentWaypointIndex].waitTime;

        // WAŻNE: Wyłącz automatyczną rotację agenta.
        // Od teraz my kontrolujemy rotację bota.
        agent.updateRotation = false;
    }

    /// <summary>
    /// Przełącza bota w stan RUCHU i ustawia nowy cel.
    /// </summary>
    private void StartMovingToNextWaypoint()
    {
        currentState = BotState.Moving;

        // WAŻNE: Włącz z powrotem automatyczną rotację agenta.
        // Agent będzie teraz sam obracał bota w kierunku marszu.
        agent.updateRotation = true;

        // Ustaw cel w NavMeshAgent
        PatrolWaypoint targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint.point != null)
        {
            agent.SetDestination(targetWaypoint.point.position);
        }
        else
        {
            Debug.LogWarning($"[BotPatrol] Punkt o indeksie {currentWaypointIndex} jest 'null'.", this);
        }
    }

    /// <summary>
    /// Rysuje wizualizacje Gizmos w edytorze.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 1)
        {
            return;
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            PatrolWaypoint wp = waypoints[i];

            // Jeśli punkt istnieje
            if (wp.point != null)
            {
                // Rysuj sferę
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(wp.point.position, 0.5f);

                // Rysuj linię pokazującą kierunek patrzenia (NOWA FUNKCJA)
                Quaternion lookRot = Quaternion.Euler(wp.targetRotation);
                Vector3 direction = lookRot * Vector3.forward; // Konwertuj rotację na wektor
                Gizmos.color = Color.red;
                Gizmos.DrawRay(wp.point.position, direction * 2f); // Czerwona linia 2m

                // Rysuj linię do następnego punktu
                Gizmos.color = Color.white;
                int nextIndex = (i + 1) % waypoints.Count;
                Transform nextPoint = waypoints[nextIndex].point;

                if (nextPoint != null)
                {
                    Gizmos.DrawLine(wp.point.position, nextPoint.position);
                }
            }
        }
    }
}