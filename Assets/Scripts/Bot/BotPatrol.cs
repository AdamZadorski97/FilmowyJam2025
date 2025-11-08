using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events; // Wymagane dla UnityEvent z parametrem

/// <summary>
/// Kontroluje bota AI, który patroluje listę punktów (waypoints).
/// Reaguje na wykrycie gracza przez TeacherFOV i kontroluje animacje.
/// </summary>
/// 



// NOWA KLASA: Definiuje właściwości każdego punktu patrolu.
[System.Serializable]
public class PatrolWaypoint
{
    [Tooltip("Transform punktu, do którego bot pójdzie.")]
    public Transform point;

    [Tooltip("Czas (w sekundach), jaki bot spędzi na tym punkcie.")]
    public float waitTime = 0f;

    [Tooltip("Kierunek (w stopniach, kąty Eulera), w którym bot ma patrzeć PODCZAS postoju.")]
    public Vector3 targetRotation;

    [Tooltip("Nazwa parametru Triggera lub Boola w Animatorze do ustawienia podczas postoju (np. 'LookAround').")]
    public string idleAnimationTrigger = "";
}


[RequireComponent(typeof(NavMeshAgent))]
public class BotPatrol : MonoBehaviour
{
    // --- NOWY EVENT ---
    [Header("Eventy Akcji")]
    [Tooltip("Wywoływane, gdy bot złapie gracza (bliskość < catchDistance) podczas ALARMU. Oczekuje PlayerController.")]
    // UnityEvent<T> wymaga, aby PlayerController był dostępny w zasięgu (using UnityEngine; jest).
    public UnityEvent<PlayerController> OnPlayerCaught;
    // ------------------

    [Header("Referencje")]
    [SerializeField]
    private NavMeshAgent agent;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private TeacherFOV teacherFOV;

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

    [Header("Ustawienia Pościgu (NOWE)")]
    [SerializeField]
    [Tooltip("Maksymalna odległość do gracza, by uznać go za 'złapanego' i wywołać akcję.")]
    private float catchDistance = 2.0f; // Dystans 2 jednostek

    // Stringi do wywoływania animacji
    private const string ANIM_IS_WALKING = "IsWalking";

    // Zmienne wewnętrzne
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private BotState currentState;
    private Vector3 lastKnownPlayerPosition; // Ostatnia znana pozycja gracza

    private enum BotState
    {
        Moving,
        Waiting,
        Alert,
        Chase  // Pogoń do ostatniej znanej pozycji
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (agent == null || waypoints == null || waypoints.Count == 0)
        {
            if (agent == null) Debug.LogError($"[BotPatrol] Brakuje NavMeshAgent!", this);
            if (waypoints == null || waypoints.Count == 0) Debug.LogError($"[BotPatrol] Nie przypisano żadnych punktów!", this);
            enabled = false;
            return;
        }

        // SUBSKRYPCJA DO EVENTÓW Z TeacherFOV
        if (teacherFOV != null)
        {
            teacherFOV.OnPlayerDetected.AddListener(OnPlayerDetected);
            teacherFOV.OnPlayerLost.AddListener(OnPlayerLost);
        }

        StartMovingToNextWaypoint();
    }

    // Metoda wywoływana przez event TeacherFOV.OnPlayerDetected
    private void OnPlayerDetected()
    {
        // Sprawdzamy, czy wykryto gracza
        if (teacherFOV.DetectedPlayer != null)
        {
            // Zapisz pozycję gracza i rozpocznij pościg (lub odśwież cel, jeśli już gonimy)
            lastKnownPlayerPosition = teacherFOV.DetectedPlayer.transform.position;

            if (currentState != BotState.Chase)
            {
                currentState = BotState.Chase;
                StartChase(lastKnownPlayerPosition);
                Debug.Log($"[BotPatrol] GRACZ WYKRYTY! Przechodzę w stan Chase do: {lastKnownPlayerPosition}.");
            }
            else
            {
                // Jeśli już gonimy, po prostu odśwież cel
                agent.SetDestination(lastKnownPlayerPosition);
            }
        }
    }

    // Metoda wywoływana, gdy gracz zniknie z FOV
    private void OnPlayerLost()
    {
        if (currentState == BotState.Chase)
        {
            // Pozostajemy w Chase, kontynuując ruch do ostatniej znanej pozycji
            Debug.Log("[BotPatrol] Gracz stracony, ale kontynuuję do ostatniej znanej pozycji.");
        }
        else if (currentState == BotState.Alert)
        {
            // Wznawiamy patrol (jeśli byliśmy tylko w trybie Alert)
            StartMovingToNextWaypoint();
            Debug.Log("[BotPatrol] Gracz stracony. Wznawiam patrol.");
        }
    }


    private void Update()
    {
        if (waypoints.Count == 0) return;

        // Prosta maszyna stanów
        switch (currentState)
        {
            case BotState.Moving:
                SetAnimationBool(ANIM_IS_WALKING, true);
                HandleMovingState();
                break;
            case BotState.Waiting:
                SetAnimationBool(ANIM_IS_WALKING, false);
                HandleWaitingState();
                break;
            case BotState.Alert:
                SetAnimationBool(ANIM_IS_WALKING, false);
                break;
            case BotState.Chase: // NOWY STAN
                SetAnimationBool(ANIM_IS_WALKING, true);
                HandleChaseState();
                break;
        }
    }

    // --- Metody Pomocnicze do Animacji ---

    private void SetAnimationBool(string paramName, bool value)
    {
        if (animator != null) animator.SetBool(paramName, value);
    }

    private void SetAnimationTrigger(string paramName)
    {
        if (animator != null && !string.IsNullOrEmpty(paramName)) animator.SetTrigger(paramName);
    }

    // --- Obsługa Stanów ---

    private void HandleMovingState()
    {
        if (!agent.pathPending && agent.remainingDistance <= arrivalThreshold)
        {
            StartWaiting();
        }
    }

    private void HandleWaitingState()
    {
        PatrolWaypoint currentWaypoint = waypoints[currentWaypointIndex];
        Quaternion targetLookRotation = Quaternion.Euler(currentWaypoint.targetRotation);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetLookRotation,
            rotationSpeed * Time.deltaTime
        );

        waitTimer -= Time.deltaTime;

        if (waitTimer <= 0f)
        {
            if (!string.IsNullOrEmpty(currentWaypoint.idleAnimationTrigger))
            {
                animator.ResetTrigger(currentWaypoint.idleAnimationTrigger);
            }

            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            StartMovingToNextWaypoint();
        }
    }

    private void StartChase(Vector3 targetPosition)
    {
        currentState = BotState.Chase;
        agent.updateRotation = true;
        agent.isStopped = false;

        agent.SetDestination(targetPosition);
    }

    private void HandleChaseState()
    {
        // 1. Sprawdzenie warunków złapania
        PlayerController player = teacherFOV.DetectedPlayer;

        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

            if (distToPlayer <= catchDistance)
            {
                Debug.Log($"[BotPatrol] Gracz złapany w promieniu {catchDistance}m! Wywołuję zerowanie postępu.");
                OnPlayerCaught.Invoke(player); // WYWOŁANIE AKCJI ZŁAPANIA

                // Zatrzymujemy się i czekamy przed wznowieniem patrolu
                StartCoroutine(WaitAfterCatchAndResumePatrol(1.0f));
                return;
            }
        }

        // 2. Sprawdzenie, czy bot dotarł do celu (ostatniej znanej pozycji)
        if (!agent.pathPending && agent.remainingDistance <= arrivalThreshold)
        {
            Debug.Log("[BotPatrol] Dotarłem do ostatniej znanej pozycji. Wznawiam patrol.");
            StartMovingToNextWaypoint();
        }
    }

    private IEnumerator WaitAfterCatchAndResumePatrol(float delay)
    {
        agent.isStopped = true;
        SetAnimationBool(ANIM_IS_WALKING, false);

        yield return new WaitForSeconds(delay);

        // Po krótkiej chwili wracamy do patrolu, do bieżącego (ostatniego) punktu
        StartMovingToNextWaypoint();
    }

    private void StartWaiting()
    {
        currentState = BotState.Waiting;

        waitTimer = waypoints[currentWaypointIndex].waitTime;

        agent.updateRotation = false;
        agent.isStopped = true;

        SetAnimationTrigger(waypoints[currentWaypointIndex].idleAnimationTrigger);
    }

    private void StartMovingToNextWaypoint()
    {
        currentState = BotState.Moving;

        agent.updateRotation = true;
        agent.isStopped = false;

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

    // --- OnDrawGizmos (bez zmian) ---
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 1) return;

        for (int i = 0; i < waypoints.Count; i++)
        {
            PatrolWaypoint wp = waypoints[i];
            if (wp.point != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(wp.point.position, 0.5f);

                Quaternion lookRot = Quaternion.Euler(wp.targetRotation);
                Vector3 direction = lookRot * Vector3.forward;
                Gizmos.color = Color.red;
                Gizmos.DrawRay(wp.point.position + Vector3.up * 0.1f, direction * 2f);

                Gizmos.color = Color.white;
                int nextIndex = (i + 1) % waypoints.Count;
                Transform nextPoint = waypoints[nextIndex].point;

                if (nextPoint != null) Gizmos.DrawLine(wp.point.position, nextPoint.position);
            }
        }

        if (Application.isPlaying && agent != null && agent.hasPath)
        {
            Gizmos.color = Color.blue;
            Vector3 lastCorner = transform.position;
            foreach (var corner in agent.path.corners)
            {
                Gizmos.DrawLine(lastCorner, corner);
                Gizmos.DrawSphere(corner, 0.1f);
                lastCorner = corner;
            }
        }
    }
}