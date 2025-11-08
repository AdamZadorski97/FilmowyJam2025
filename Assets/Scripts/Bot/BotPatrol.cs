using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    public string idleAnimationTrigger = ""; // <--- NOWE POLE STRING
}

/// <summary>
/// Kontroluje bota AI, który patroluje listę punktów (waypoints).
/// Reaguje na wykrycie gracza przez TeacherFOV i kontroluje animacje.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BotPatrol : MonoBehaviour
{
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

    // Stringi do wywoływania animacji
    private const string ANIM_IS_WALKING = "IsWalking";

    // Zmienne wewnętrzne
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private BotState currentState;

    private enum BotState
    {
        Moving,
        Waiting,
        Alert
    }

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
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
        if (currentState != BotState.Alert)
        {
            currentState = BotState.Alert;

            // Zatrzymaj agenta i animację
            if (agent.enabled)
            {
                agent.isStopped = true;
            }

            // Wyłącz animację chodu
            SetAnimationBool(ANIM_IS_WALKING, false);
            Debug.Log("[BotPatrol] GRACZ WYKRYTY! Przechodzę w stan Alert.");
        }
    }

    // Metoda wywoływana, gdy gracz zniknie z FOV
    private void OnPlayerLost()
    {
        if (currentState == BotState.Alert)
        {
            // Wznów patrol z miejsca, w którym się zatrzymał
            if (agent.enabled)
            {
                agent.isStopped = false;
            }

            if (waypoints[currentWaypointIndex].waitTime > 0f && agent.remainingDistance < arrivalThreshold * 2f)
            {
                StartWaiting();
            }
            else
            {
                StartMovingToNextWaypoint();
            }
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
                SetAnimationBool(ANIM_IS_WALKING, true); // <--- ANIMACJA WALK
                HandleMovingState();
                break;
            case BotState.Waiting:
                SetAnimationBool(ANIM_IS_WALKING, false); // <--- ANIMACJA IDLE
                HandleWaitingState();
                break;
            case BotState.Alert:
                SetAnimationBool(ANIM_IS_WALKING, false); // <--- ANIMACJA IDLE (Alarm)
                // W tym stanie nic nie robimy poza animacją
                break;
        }
    }

    /// <summary>
    /// Wywołuje parametr bool w Animatorze za pomocą stringa.
    /// </summary>
    private void SetAnimationBool(string paramName, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(paramName, value);
        }
    }

    /// <summary>
    /// Wywołuje Trigger w Animatorze za pomocą stringa.
    /// </summary>
    private void SetAnimationTrigger(string paramName)
    {
        if (animator != null && !string.IsNullOrEmpty(paramName))
        {
            animator.SetTrigger(paramName);
        }
    }


    private void HandleMovingState()
    {
        // Sprawdź, czy dotarliśmy do celu
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

        // Odliczaj czas
        waitTimer -= Time.deltaTime;

        // Jeśli czas minął, ruszaj do następnego punktu
        if (waitTimer <= 0f)
        {
            // Resetuj trigger animacji customowej przed ruszeniem
            if (!string.IsNullOrEmpty(currentWaypoint.idleAnimationTrigger))
            {
                animator.ResetTrigger(currentWaypoint.idleAnimationTrigger);
            }

            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            StartMovingToNextWaypoint();
        }
    }

    /// <summary>
    /// Przełącza bota w stan OCZEKIWANIA i wywołuje animację z punktu.
    /// </summary>
    private void StartWaiting()
    {
        currentState = BotState.Waiting;

        // Ustaw czas oczekiwania
        waitTimer = waypoints[currentWaypointIndex].waitTime;

        // Dezaktywuj automatyczną rotację agenta (NavMeshAgent) i zatrzymaj go
        agent.updateRotation = false;
        agent.isStopped = true;

        // Wywołaj animację zdefiniowaną w punkcie (jako Trigger lub Bool)
        SetAnimationTrigger(waypoints[currentWaypointIndex].idleAnimationTrigger);
    }

    /// <summary>
    /// Przełącza bota w stan RUCHU i ustawia nowy cel.
    /// </summary>
    private void StartMovingToNextWaypoint()
    {
        currentState = BotState.Moving;

        // Aktywuj automatyczną rotację agenta i wznów ruch
        agent.updateRotation = true;
        agent.isStopped = false;

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

        // Rysuj punkty i połączenia
        for (int i = 0; i < waypoints.Count; i++)
        {
            PatrolWaypoint wp = waypoints[i];

            // Jeśli punkt istnieje
            if (wp.point != null)
            {
                // Rysuj sferę punktu
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(wp.point.position, 0.5f);

                // Rysuj linię pokazującą kierunek patrzenia podczas postoju
                Quaternion lookRot = Quaternion.Euler(wp.targetRotation);
                Vector3 direction = lookRot * Vector3.forward;
                Gizmos.color = Color.red;
                Gizmos.DrawRay(wp.point.position + Vector3.up * 0.1f, direction * 2f);

                // Rysuj linię do następnego punktu (kolejność patrolu)
                Gizmos.color = Color.white;
                int nextIndex = (i + 1) % waypoints.Count;
                Transform nextPoint = waypoints[nextIndex].point;

                if (nextPoint != null)
                {
                    Gizmos.DrawLine(wp.point.position, nextPoint.position);
                }
            }
        }

        // Rysuj aktualną trasę NavMeshAgent (tylko w trybie Play)
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