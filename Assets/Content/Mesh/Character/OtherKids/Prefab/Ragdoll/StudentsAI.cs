using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX;


/*if (Edyp)
{
    animator.SetBool("IsSitting", true);
    return;
}*/

public class StudentsAI : MonoBehaviour
{
    // NOWA, STATYCZNA BLOKADA: Gwarantuje, że tylko jedna postać angażuje gracza
    private static bool isPlayerEngaged = false;

    // === Nastawienie Postaci (Losowane) ===
    public enum Attitude { Friendly, Neutral, Hostile }

    [Header("Interakcje z Graczem")]
    public Attitude attitude;
    public float HostileDetectionRange = 10f;
    public float FriendlyDetectionRange = 8f;

    // === Walka i HP ===
    [Header("Walka i HP")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private float fleeDistance = 20f; // Minimalny dystans, na jaki ucieka postać
    [SerializeField] private float FleeSpeed = 7.0f;   // Prędkość ucieczki (szybciej niż normalny bieg)
    private bool isStunnedAfterHit = false; // Zapobieganie reakcjom na spam ciosami

    // === Ustawienia publiczne ===
    [Header("Ruch i Czas")]
    public float WanderRadius = 30f;
    public float MinChatTime = 3f;
    public float MaxChatTime = 8f;
    // Domyślne prędkości (dostosuj do swoich animacji)
    private float walkSpeed = 2.5f;
    private float runSpeed = 5.0f;

    // === Ustawienia Grupowania i Drzwi ===
    public float GroupChatRadius = 5f;
    public float CircleRadius = 1.5f;
    public float DoorCheckDistance = 1.0f;

    // === Komponenty ===
    private NavMeshAgent agent;
    private Animator animator;
    private Transform playerTransform;

    // === Maszyna Stanów (DODANO Fleeing i Fighting) ===
    private enum AIState { Idle_Chat, Wandering, InteractAtPOI, PlayerEncounter, Fleeing, Fighting }
    private AIState currentState = AIState.Wandering;

    // Zmienne stanu
    private float chatDuration;
    private InteractionPoint currentPOI = null;
    public bool Edyp = false;

    [SerializeField] private VisualEffect Blood;

    void Start()
    {
        // --- LOSOWANIE NASTAWIENIA NA STARCIE ---
        int randomAttitudeIndex = Random.Range(0, 3);
        attitude = (Attitude)randomAttitudeIndex;
        Debug.Log(gameObject.name + " ma wylosowane nastawienie: " + attitude.ToString());
        // ----------------------------------------

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth; // Ustawienie początkowego zdrowia

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        if (agent == null || animator == null)
        {
            Debug.LogError("Brak komponentów NavMeshAgent lub Animator na obiekcie " + gameObject.name + ". AI nie będzie działać.");
            enabled = false;
            return;
        }

        agent.speed = walkSpeed;

        if (Edyp)
        {
            animator.SetBool("IsSitting", true);
            return;
        }

        StartCoroutine(AILoop());
    }

    void Update()
    {
        // Sprawdź gracza TYLKO jeśli postać jest wolna i w stanie Wandering/Idle
        if (!Edyp && playerTransform != null && (currentState == AIState.Wandering || currentState == AIState.Idle_Chat || currentState == AIState.InteractAtPOI))
        {
            if (CheckForPlayerDiscovery())
            {
                // Natychmiastowe przełączenie na stan spotkania
                StopAllCoroutines();
                currentState = AIState.PlayerEncounter;
                StartCoroutine(AILoop());
            }
        }
    }

    // Metoda wywoływana przez PlayerHitbox, gdy student zostanie uderzony
    public void GetHit(int damage)
    {
        if (isStunnedAfterHit || currentHealth <= 0) return;

        currentHealth -= damage;
        isStunnedAfterHit = true;

        // === DODANO: ANIMACJA UDERZENIA I BLOKADA RUCHU ===
        if (animator != null) animator.SetTrigger("TakeHit");
        if (agent != null && agent.enabled) agent.isStopped = true;
        // ===================================================

        // Zakładam, że "Blood" to VisualEffect
        Blood.transform.rotation = Quaternion.Euler(
            Random.Range(-180f, 180f),  // Losowa rotacja wokół osi X
            Random.Range(-60f, 60f),    // Losowa rotacja wokół osi Y
            Random.Range(-180f, 180f)   // Losowa rotacja wokół osi Z
        );
        Blood.Play();

        // Zatrzymujemy bieżące działanie (w tym Wandering/Fleeing)
        StopAllCoroutines();

        Debug.Log(gameObject.name + " otrzymał " + damage + " obrażeń. HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log(gameObject.name + " został pokonany!");
            agent.isStopped = true;
            if (isPlayerEngaged) isPlayerEngaged = false;
            animator.enabled = false;
            enabled = false;
            UIcontrollerPopUp.Instance.IncraseRep();
            return;
        }

        if (attitude == Attitude.Hostile)
        {
            currentState = AIState.Fighting;
        }
        else if (attitude == Attitude.Friendly || attitude == Attitude.Neutral)
        {
            currentState = AIState.Fleeing;
        }

        // Ponowne uruchomienie pętli AI z nowym stanem
        StartCoroutine(AILoop());

        // Krótki "stun" zapobiegający spamowaniu atakami (wznowi ruch agenta)
        StartCoroutine(StunAfterHitCoroutine(0.5f));
    }

    private IEnumerator StunAfterHitCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        isStunnedAfterHit = false;
    }


    private bool CheckForPlayerDiscovery()
    {
        // ... (Logika wykrywania, bez zmian)
        if (isPlayerEngaged) return false;
        if (playerTransform == null) return false;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (attitude == Attitude.Hostile && distance <= HostileDetectionRange)
        {
            return true;
        }

        if (attitude == Attitude.Friendly && distance <= FriendlyDetectionRange)
        {
            return true;
        }

        return false;
    }

    private IEnumerator AILoop()
    {
        while (true)
        {
            switch (currentState)
            {
                case AIState.Idle_Chat:
                    yield return HandleIdleChat();
                    break;
                case AIState.Wandering:
                    yield return HandleWandering();
                    break;
                case AIState.InteractAtPOI:
                    yield return HandlePOIInteraction();
                    break;
                case AIState.PlayerEncounter:
                    // To wywoła albo rozmowę (Friendly), ignorowanie (Neutral), albo przejście do walki (Hostile)
                    yield return HandlePlayerEncounter();
                    break;
                case AIState.Fleeing:
                    yield return HandleFleeing();
                    break;
                case AIState.Fighting:
                    //yield return HandleFighting();
                    yield return HandleFleeing();
                    break;
            }

            // --- ZARZĄDZANIE STANAMI PO ZAKOŃCZENIU AKCJI ---
            // Po Fleeing, Fighting i PlayerEncounter (jeśli nie jest Fighting) wracamy do Wandering.
            if (currentState != AIState.Fighting)
            {
                currentState = ChooseNextState();
            }

            yield return null;
        }
    }

    // =====================================
    // === STAN: UCIECZKA (Fleeing) ===
    // =====================================
    private IEnumerator HandleFleeing()
    {
        Debug.Log(gameObject.name + " (" + attitude + ") UCIECZKA po ataku!");

        // 1. Ustawienie parametrów biegu
        agent.speed = FleeSpeed;
        animator.SetFloat("Y", 1.0f); // Bieganie (zakładamy, że 1.0f to animacja biegu)
        agent.isStopped = false;

        // Pętla kontynuuje, dopóki Gracz jest bliżej niż bezpieczna odległość LUB agent nie ma ścieżki
        while (Vector3.Distance(transform.position, playerTransform.position) < fleeDistance)
        {
            // 2. Dynamiczne wyznaczanie kierunku ucieczki (przeciwny do aktualnej pozycji Gracza)
            Vector3 fleeDirection = (transform.position - playerTransform.position).normalized;
            Vector3 safePoint = transform.position + fleeDirection * fleeDistance;
            Vector3 targetDestination;

            // 3. Ustawienie nowego celu na NavMesh
            if (GetPointOnNavMesh(safePoint, fleeDistance / 2, out targetDestination))
            {
                agent.SetDestination(targetDestination);
            }

            // 4. Sprawdzenie drzwi i ścieżki
            if (agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                CheckForDoor();
            }

            yield return null;
        }

        // 5. Koniec ucieczki - powrót do stanu Idle/Wandering
        agent.isStopped = true;
        animator.SetFloat("Y", 0f);

        // Powrót do stanu Wandering
        currentState = AIState.Wandering;
    }

    // =====================================
    // === STAN: WALKA (Fighting) ===
    // =====================================
    private IEnumerator HandleFighting()
    {
        Debug.Log(gameObject.name + " (Hostile) wchodzi w ciągły tryb walki!");

        // CIĄGŁA PĘTLA WALKI
        while (currentHealth > 0 && playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) < HostileDetectionRange * 2)
        {
            if (isPlayerEngaged)
            {
                // Czekamy, jeśli inny student już angażuje gracza
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Wykonaj sekwencję pościgu i ataku
            yield return CombatSequence();

            // Krótka pauza przed wznowieniem ataku/pościgu
            yield return new WaitForSeconds(0.5f);
        }

        // Jeśli gracz uciekł (za daleko) lub HP = 0
        Debug.Log(gameObject.name + " kończy walkę.");
        currentState = AIState.Wandering;
    }

    // WŁAŚCIWA SEKWENCJA POŚCIGU I ATAKU (Zoptymalizowana)
    private IEnumerator CombatSequence()
    {
        // Upewnij się, że Gracz istnieje i postać ma HP
        if (playerTransform == null || currentHealth <= 0)
        {
            isPlayerEngaged = false; // Zwalniamy blokadę na wszelki wypadek
            yield break;
        }

        // Zapobiegamy przejęciu kontroli przez inny wątek po wznowieniu pętli Fighting
        if (isPlayerEngaged)
        {
            yield break;
        }

        isPlayerEngaged = true; // ZAJMUJEMY BLOKADĘ

        float targetAttackDistance = 1.5f; // Używamy większego marginesu dla NavMesh

        // --- FAZA POŚCIGU ---
        agent.isStopped = false;
        agent.SetDestination(playerTransform.position);
        animator.SetFloat("Y", 1.0f); // Bieganie do gracza
        agent.speed = runSpeed;

        // 1. Aktywne śledzenie i ruch, dopóki cel nie jest w zasięgu
        while (Vector3.Distance(transform.position, playerTransform.position) > targetAttackDistance)
        {
            // Aktualizuj cel w pętli, aby śledzić Gracza (nawet jeśli Gracz się porusza)
            agent.SetDestination(playerTransform.position);

            // Jedno sprawdzenie drzwi na iterację
            if (agent.pathStatus == NavMeshPathStatus.PathPartial)
            {
                CheckForDoor();
            }

            // Jeśli ścieżka się zepsuje na dłużej, wychodzimy z pościgu
            if (!agent.hasPath && agent.remainingDistance > targetAttackDistance)
            {
                Debug.LogWarning(gameObject.name + " utracił ścieżkę do Gracza. Rezygnuję z pościgu.");
                isPlayerEngaged = false;
                yield break;
            }

            yield return null;
        }

        // --- FAZA ATAKU ---
        agent.isStopped = true;

        // 1. Natychmiastowy obrót do Gracza
        Vector3 lookDirection = playerTransform.position - transform.position;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        // 2. Atak (3 ciosy)
        for (int i = 0; i < 3; i++)
        {
            // Szybkie odświeżenie rotacji przed każdym atakiem
            lookDirection = playerTransform.position - transform.position;
            lookDirection.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 30f);

            animator.SetTrigger("Attack"); // Odpalenie triggera "Attack"

            // Czas na animację ataku + lekkie opóźnienie
            yield return new WaitForSeconds(1.5f);
        }

        isPlayerEngaged = false; // ZWALNIAMY BLOKADĘ
    }
    // ===========================================
    // === STAN: Obsługa spotkania z graczem (zmodyfikowany) ===
    // ===========================================
    private IEnumerator HandlePlayerEncounter()
    {
        if (attitude == Attitude.Hostile)
        {
            // Hostile przechodzi bezpośrednio do ciągłego trybu walki
            Debug.Log(gameObject.name + " (Hostile) wykrył Gracza. Przechodzę w tryb Walki.");
            currentState = AIState.Fighting;
            yield break;
        }

        // --- Logika dla Friendly/Neutral (pozostała bez zmian) ---
        if (isPlayerEngaged) yield break;
        isPlayerEngaged = true;

        agent.isStopped = true;
        animator.SetFloat("Y", 0f);

        Vector3 lookDirection = playerTransform.position - transform.position;
        lookDirection.y = 0;
        transform.rotation = Quaternion.LookRotation(lookDirection);

        // Reszta logiki Friendly/Neutral (podchodzenie do rozmowy, ignorowanie)
        switch (attitude)
        {
            case Attitude.Friendly:
                // ... (Logic for Friendly approach and Talk) ...
                float talkDistance = 2.0f;
                agent.isStopped = false;
                agent.SetDestination(playerTransform.position);
                animator.SetFloat("Y", 0.5f);
                agent.speed = walkSpeed;

                yield return new WaitUntil(() => Vector3.Distance(transform.position, playerTransform.position) <= talkDistance || !agent.hasPath);

                agent.isStopped = true;
                animator.SetFloat("Y", 0f);
                lookDirection = playerTransform.position - transform.position;
                lookDirection.y = 0;
                transform.rotation = Quaternion.LookRotation(lookDirection);

                animator.SetTrigger("Talk");
                yield return new WaitForSeconds(3f);
                break;

            case Attitude.Neutral:
                Debug.Log(gameObject.name + " (Neutral) ignoruje Graczem.");
                yield return new WaitForSeconds(0.5f);
                break;

            default: // Powinno być obsłużone przez warunek if (attitude == Attitude.Hostile)
                break;
        }

        isPlayerEngaged = false; // ZWALNIAMY BLOKADĘ
    }


    private AIState ChooseNextState()
    {
        if (Random.value < 0.3f && TryFindAvailablePOI())
        {
            return AIState.InteractAtPOI;
        }

        if (Random.value < 0.3f)
        {
            return AIState.Idle_Chat;
        }

        return AIState.Wandering;
    }


    // ===================================
    // === STAN: BEZCZYNNOŚĆ/CHATOWANIE === (BEZ ZMIAN)
    // ===================================

    private IEnumerator HandleIdleChat()
    {
        // ... (Kod Idle/Chat bez zmian)
        agent.isStopped = true;
        animator.SetFloat("Y", 0f);
        chatDuration = Random.Range(MinChatTime, MaxChatTime);

        Vector3 meetingPoint;
        Vector3 circlePosition;

        if (TryFormGroup(out meetingPoint, out circlePosition))
        {
            agent.isStopped = false;
            agent.SetDestination(circlePosition);
            animator.SetFloat("Y", 0.5f);

            yield return new WaitUntil(() =>
                !agent.pathPending &&
                (agent.remainingDistance < 0.5f || agent.pathStatus == NavMeshPathStatus.PathPartial));

            agent.isStopped = true;
            animator.SetFloat("Y", 0f);

            Vector3 lookDirection = meetingPoint - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        yield return new WaitForSeconds(chatDuration);
    }

    private bool TryFormGroup(out Vector3 meetingCenter, out Vector3 myPosition)
    {
        // ... (Kod TryFormGroup bez zmian)
        Collider[] colliders = Physics.OverlapSphere(transform.position, GroupChatRadius);
        List<StudentsAI> groupMembers = new List<StudentsAI>();
        Vector3 centerSum = transform.position;
        groupMembers.Add(this);

        foreach (Collider col in colliders)
        {
            StudentsAI otherStudent = col.GetComponent<StudentsAI>();
            if (otherStudent != null && otherStudent != this && otherStudent.currentState == AIState.Idle_Chat)
            {
                groupMembers.Add(otherStudent);
                centerSum += otherStudent.transform.position;
            }
        }

        if (groupMembers.Count < 2)
        {
            meetingCenter = transform.position;
            myPosition = transform.position;
            return false;
        }

        Vector3 rawCenter = centerSum / groupMembers.Count;
        if (!GetPointOnNavMesh(rawCenter, WanderRadius, out meetingCenter))
        {
            meetingCenter = transform.position;
            myPosition = transform.position;
            return false;
        }
        int myIndex = groupMembers.IndexOf(this);
        int totalMembers = groupMembers.Count;
        float angleStep = 360f / totalMembers;
        float myAngle = myIndex * angleStep;
        float angleInRadians = myAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Cos(angleInRadians) * CircleRadius,
            0,
            Mathf.Sin(angleInRadians) * CircleRadius
        );
        Vector3 finalPosition = meetingCenter + offset;

        if (GetPointOnNavMesh(finalPosition, 1f, out myPosition))
        {
            return true;
        }
        myPosition = transform.position;
        return false;
    }


    // =============================
    // === STAN: WĘDRÓWKA LOSOWA === (BEZ ZMIAN)
    // =============================

    private IEnumerator HandleWandering()
    {
        Vector3 randomDestination;

        if (GetPointOnNavMesh(transform.position + Random.insideUnitSphere * WanderRadius, WanderRadius, out randomDestination))
        {
            agent.isStopped = false;
            agent.SetDestination(randomDestination);

            float moveSpeed = Random.Range(0f, 1f) > 0.8f ? 1.0f : 0.5f;
            animator.SetFloat("Y", moveSpeed);
            agent.speed = (moveSpeed == 1.0f) ? runSpeed : walkSpeed;

            yield return null;

            while (!agent.pathPending && agent.remainingDistance > 0.5f && agent.hasPath)
            {
                if (agent.pathStatus == NavMeshPathStatus.PathPartial)
                {
                    if (CheckForDoor())
                    {
                        yield return null;
                    }
                }
                yield return null;
            }

            agent.isStopped = true;
            animator.SetFloat("Y", 0f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }
    }


    // ===================================
    // === STAN: INTERAKCJA W PUNKCIE POI === (BEZ ZMIAN)
    // ===================================

    private IEnumerator HandlePOIInteraction()
    {
        if (currentPOI == null) yield break;

        Vector3 finalTargetPosition = currentPOI.transform.position;
        if (currentPOI.Type == InteractionPoint.InteractionType.GroupChat)
        {
            Vector3 tempCenter, tempPosition;
            if (TryFormGroup(out tempCenter, out tempPosition))
            {
                finalTargetPosition = tempPosition;
            }
        }
        agent.isStopped = false;
        agent.SetDestination(finalTargetPosition);
        animator.SetFloat("Y", 0.5f);

        yield return new WaitUntil(() => !agent.pathPending && (agent.remainingDistance < 0.2f || agent.pathStatus == NavMeshPathStatus.PathPartial));

        if (agent.pathStatus == NavMeshPathStatus.PathPartial && !CheckForDoor())
        {
            currentPOI.CurrentUsers--;
            currentPOI = null;
            yield break;
        }
        agent.isStopped = true;
        animator.SetFloat("Y", 0f);

        if (currentPOI.Type != InteractionPoint.InteractionType.GroupChat)
        {
            transform.position = currentPOI.transform.position;
            transform.rotation = currentPOI.transform.rotation;
        }
        else
        {
            Vector3 lookDirection = currentPOI.transform.position - transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        float interactionTime = Random.Range(MinChatTime, MaxChatTime);

        switch (currentPOI.Type)
        {
            case InteractionPoint.InteractionType.Sit:
                animator.SetBool("IsSitting", true);
                break;
            case InteractionPoint.InteractionType.GroupChat:
                animator.SetTrigger("Talk");
                break;
            case InteractionPoint.InteractionType.IdleAnimation:
                animator.SetTrigger("Fidget");
                break;
        }

        yield return new WaitForSeconds(interactionTime);
        if (currentPOI.Type == InteractionPoint.InteractionType.Sit)
        {
            animator.SetBool("IsSitting", false);
        }
        currentPOI.CurrentUsers--;
        currentPOI = null;
    }

    private bool TryFindAvailablePOI()
    {
        // ... (Kod TryFindAvailablePOI bez zmian)
        InteractionPoint[] allPOIs = FindObjectsOfType<InteractionPoint>();
        InteractionPoint bestPOI = null;
        float closestDistance = WanderRadius;

        foreach (InteractionPoint poi in allPOIs)
        {
            if (poi.CurrentUsers < poi.MaxUsers)
            {
                float distance = Vector3.Distance(transform.position, poi.transform.position);

                if (distance < closestDistance)
                {
                    NavMeshPath path = new NavMeshPath();
                    if (agent.CalculatePath(poi.transform.position, path) && path.status == NavMeshPathStatus.PathComplete)
                    {
                        closestDistance = distance;
                        bestPOI = poi;
                    }
                }
            }
        }

        if (bestPOI != null)
        {
            bestPOI.CurrentUsers++;
            currentPOI = bestPOI;
            return true;
        }

        return false;
    }


    // =============================
    // === METODY POMOCNICZE === (BEZ ZMIAN)
    // =============================

    private bool CheckForDoor()
    {
        Vector3 checkPosition = transform.position + transform.forward * 0.5f;
        Collider[] hitColliders = Physics.OverlapSphere(checkPosition, DoorCheckDistance);

        foreach (var hitCollider in hitColliders)
        {
            DoorController door = hitCollider.GetComponent<DoorController>();

            if (door != null)
            {
                // ToggleDoor jest wywoływany tylko raz
                door.ToggleDoor();
                return true;
            }
        }
        return false;
    }

    private bool GetPointOnNavMesh(Vector3 center, float range, out Vector3 result)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(center, out hit, range, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }
}