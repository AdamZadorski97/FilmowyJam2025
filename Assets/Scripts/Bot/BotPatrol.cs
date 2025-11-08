using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Definiuje właściwości każdego punktu patrolu.
/// </summary>
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
    // --- EVENT Z PARAMETREM ---
    [Header("Eventy Akcji")]
    [Tooltip("Wywoływane, gdy bot złapie gracza (bliskość < catchDistance). Oczekuje PlayerController.")]
    public UnityEvent<PlayerController> OnPlayerCaught;
    // --------------------------

    [Header("Referencje")]
    [SerializeField]
    private NavMeshAgent agent;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private TeacherFOV teacherFOV;

    [Header("Ustawienia Prędkości")]
    [Tooltip("Prędkość poruszania się bota podczas patrolu (Walking).")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [Tooltip("Prędkość poruszania się bota podczas pościgu (Running).")]
    [SerializeField] private float chaseSpeed = 6.0f;

    [Header("Ustawienia Patrolu")]
    [SerializeField]
    [Tooltip("Lista punktów patrolu.")]
    private List<PatrolWaypoint> waypoints;

    [SerializeField]
    [Tooltip("Jak blisko bot musi podejść do punktu.")]
    private float arrivalThreshold = 0.5f;

    [SerializeField]
    [Tooltip("Jak szybko bot obraca się w docelowy kierunek.")]
    private float rotationSpeed = 5f;

    [Header("Ustawienia Złapania i Pościgu")]
    [SerializeField]
    [Tooltip("Maksymalna odległość do gracza, by uznać go za 'złapanego'.")]
    private float catchDistance = 2.0f;

    [SerializeField]
    [Tooltip("Czas stunu/mrugania gracza (np. 5.0s).")]
    private float stunDuration = 5.0f;

    [SerializeField]
    [Tooltip("Czas cooldownu nauczyciela po złapaniu tego gracza, zanim będzie mógł go gonić ponownie (np. 15.0s).")]
    private float playerChaseCooldown = 15.0f;

    [Tooltip("Czas trwania animacji ataku bota, po którym wznawia patrol (np. 2.0s).")]
    [SerializeField] private float attackAnimationDuration = 2.0f;

    [Tooltip("Czas kontynuacji pościgu po straceniu gracza (np. 5.0s).")]
    [SerializeField] private float chaseRefreshTime = 5.0f;
    [Tooltip("Czas odpoczynku po nieudanym pościgu (np. 3.0s).")]
    [SerializeField] private float restDuration = 3.0f;

    [SerializeField]
    [Tooltip("Nazwa triggera animacji ataku (zwykłe złapanie).")]
    private string attackTrigger = "Attack";
    private string softAttackTrigger = "SoftAttack";
    [SerializeField]
    [Tooltip("Nazwa triggera dla animacji odpoczynku (Rest/Search).")]
    private string restTrigger = "Rest";

    [SerializeField]
    [Tooltip("Nazwa triggera dla animacji ataku (Gdy gracz traci OSTATNIE życie).")]
    private string finalAttackTrigger = "FinalAttack";
    public float finalAttackWaitTime;
    private bool isOnAttackCooldown = false;

    // --- NOWA STAŁA DLA PARAMETRU FLOAT ---
    private const string ANIM_SPEED_FLOAT = "Speed";

    // Zmienne wewnętrzne
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private float chaseTimer;
    private BotState currentState;
    private Vector3 lastKnownPlayerPosition;

    private enum BotState
    {
        Moving,
        Waiting,
        Alert,
        Chase,
        Searching,
        Resting
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        ScoreManager.SetBotPatrolReference(this);
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

        if (teacherFOV != null)
        {
            teacherFOV.OnPlayerDetected.AddListener(OnPlayerDetected);
            teacherFOV.OnPlayerLost.AddListener(OnPlayerLost);
        }

        agent.speed = patrolSpeed;

        StartMovingToNextWaypoint();
    }

    private void Update()
    {
        if (waypoints.Count == 0) return;

        // Ustawienie Float Speed w każdej klatce, z wyjątkiem stanów zastoju
        if (currentState != BotState.Waiting && currentState != BotState.Alert && currentState != BotState.Resting)
        {
            SetAnimationSpeedFloat(agent.velocity.magnitude);
        }
        else
        {
            SetAnimationSpeedFloat(0f);
        }

        switch (currentState)
        {
            case BotState.Moving:
                HandleMovingState();
                break;
            case BotState.Waiting:
                HandleWaitingState();
                break;
            case BotState.Chase:
                HandleChaseState();
                break;
            case BotState.Searching:
                HandleSearchingState();
                break;
            case BotState.Resting:
                HandleRestingState();
                break;
            case BotState.Alert:
            default:
                break;
        }
    }

    // --- Metody Pomocnicze do Animacji ---

    /// <summary>
    /// Ustawia wartość parametru Float 'Speed' w Animatorze.
    /// </summary>
    private void SetAnimationSpeedFloat(float speed)
    {
        if (animator != null) animator.SetFloat(ANIM_SPEED_FLOAT, speed);
    }

    // Usunięta metoda SetAnimationBool, ponieważ używamy Floata

    private void SetAnimationTrigger(string paramName)
    {
        if (animator != null && !string.IsNullOrEmpty(paramName)) animator.SetTrigger(paramName);
    }

    /// <summary>
    /// Wywoływane przez ScoreManager, gdy gracz straci ostatnie życie.
    /// </summary>
    public void TriggerFinalAttack()
    {
        agent.isStopped = true;
        SoundManager.Instance.PlaySFX("baseball");
        StartCoroutine(TriggerFinalAttackEnum());
    }
    IEnumerator TriggerFinalAttackEnum()
    {
        waypoints = null;
        SetAnimationTrigger(attackTrigger);
        yield return new WaitForSeconds(finalAttackWaitTime);

        if (!string.IsNullOrEmpty(finalAttackTrigger))
        {
            SetAnimationTrigger(finalAttackTrigger);
            currentState = BotState.Alert;
            
            SetAnimationSpeedFloat(0f);
            agent.updateRotation = false;
        }
    }


    // --- Obsługa Eventów FOV ---

    private void OnPlayerDetected()
    {
        if (teacherFOV.DetectedPlayer != null)
        {
            if (teacherFOV.DetectedPlayer.IsInCooldown()) return;

            lastKnownPlayerPosition = teacherFOV.DetectedPlayer.transform.position;
            chaseTimer = chaseRefreshTime;

            if (currentState != BotState.Chase)
            {
                currentState = BotState.Chase;
                StartChase(lastKnownPlayerPosition);
            }
            else
            {
                agent.SetDestination(lastKnownPlayerPosition);
            }
        }
    }

    private void OnPlayerLost()
    {
        if (currentState == BotState.Chase)
        {
            currentState = BotState.Searching;
            Debug.Log("[BotPatrol] Gracz stracony. Kontynuuję pościg (Search) przez 5s.");
        }
    }

    // --- Obsługa Stanów ---

    private void StartChase(Vector3 targetPosition)
    {
        currentState = BotState.Chase;
        agent.updateRotation = true;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(targetPosition);
    }

    private void HandleChaseState()
    {
        PlayerController player = teacherFOV.DetectedPlayer;

        if (isOnAttackCooldown) return;
        if (player != null && player.IsInCooldown()) return;

        if (player != null)
        {
            lastKnownPlayerPosition = player.transform.position;
            agent.SetDestination(lastKnownPlayerPosition);

            if (!isOnAttackCooldown)
            {
                float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

                if (distToPlayer <= catchDistance)
                {
                    player.SetStunned(stunDuration, playerChaseCooldown);

                    SetAnimationTrigger(softAttackTrigger);
                    isOnAttackCooldown = true;

                    OnPlayerCaught.Invoke(player);

                    StartCoroutine(WaitAfterCatchAndResumePatrol(attackAnimationDuration));
                    return;
                }
            }
        }
        else
        {
            OnPlayerLost();
        }
    }

    private void HandleSearchingState()
    {
        chaseTimer -= Time.deltaTime;

        if (!agent.pathPending && agent.remainingDistance <= arrivalThreshold)
        {
            Debug.Log("[BotPatrol] Dotarłem do ostatniej znanej pozycji. Odpoczynek.");
            StartResting();
            return;
        }

        if (chaseTimer <= 0f)
        {
            Debug.Log("[BotPatrol] Czas na szukanie minął. Odpoczynek.");
            StartResting();
            return;
        }
    }

    private void StartResting()
    {
        currentState = BotState.Resting;
        agent.isStopped = true;
        agent.speed = patrolSpeed;

        SetAnimationTrigger(restTrigger);

        StartCoroutine(RestCoroutine());
    }

    private IEnumerator RestCoroutine()
    {
        yield return new WaitForSeconds(restDuration);

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        StartMovingToNextWaypoint();
    }

    private void HandleRestingState()
    {
        // Stan jest obsługiwany przez korutynę
    }

    private void StartMovingToNextWaypoint()
    {
        currentState = BotState.Moving;

        agent.updateRotation = true;
        agent.isStopped = false;
        agent.speed = patrolSpeed;

        PatrolWaypoint targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint.point != null)
        {
            agent.SetDestination(targetWaypoint.point.position);
        }
    }

    private void HandleMovingState()
    {
        if (!agent.pathPending && agent.remainingDistance <= arrivalThreshold)
        {
            StartWaiting();
        }
    }

    private void StartWaiting()
    {
        currentState = BotState.Waiting;

        waitTimer = waypoints[currentWaypointIndex].waitTime;

        agent.updateRotation = false;
        agent.isStopped = true;

        SetAnimationTrigger(waypoints[currentWaypointIndex].idleAnimationTrigger);
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

    private IEnumerator WaitAfterCatchAndResumePatrol(float delay)
    {
        agent.isStopped = true;
        SetAnimationSpeedFloat(0f); // Zatrzymanie animacji
        agent.updateRotation = false;

        yield return new WaitForSeconds(delay);

        agent.updateRotation = true;

        isOnAttackCooldown = false;

        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        StartMovingToNextWaypoint();
    }

    // --- OnDrawGizmos ---
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