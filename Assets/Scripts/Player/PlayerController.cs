using DG.Tweening;
using InControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using XInputDotNetPure;
using UnityEngine.Events;

public class PlayerController : MonoBehaviour
{
    public InputDevice InputDevice { get; set; }

    public int playerID;

    [SerializeField] private NavMeshAgent agent;

    // !!! KLUCZOWY ELEMENT: Referencja do transformu kamery gracza !!!
    public Transform playerCameraTransform;

    // !!! WAŻNE: Mesh/Wizualny Model Postaci !!!
    [SerializeField] private GameObject playerMesh;
    private Renderer playerRenderer; // NOWY: Referencja do renderera modelu

    [Header("Eventy Akcji")]
    public UnityEvent OnActionStarted;
    public UnityEvent OnActionFinished;

    [Header("Ustawienia Prędkości")]
    [SerializeField] private float baseMoveSpeed = 5f; // Normalna prędkość chodu
    [SerializeField] private float runSpeed = 8f;      // Prędkość biegania
    [SerializeField] private float sneakSpeed = 2.5f;  // Prędkość skradania
    [SerializeField] private float accelerationRate = 15f; // Akceleracja NavMeshAgenta (fizyczna)

    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;

    [Header("Czas Trwania Akcji")]
    [SerializeField] private float punchDuration = 0.5f;
    [SerializeField] private float kickDuration = 0.8f;

    // ZMODYFIKOWANE: USTAWIENIA WALKI Z HITBOXAMI
    [Header("Ustawienia Walki")]
    [Tooltip("Obiekt Hitboxa dla ciosu pięścią (musi być dzieckiem gracza i mieć PlayerHitbox.cs).")]
    [SerializeField] private GameObject punchHitboxObject;
    [Tooltip("Obiekt Hitboxa dla kopnięcia (musi być dzieckiem gracza i mieć PlayerHitbox.cs).")]
    [SerializeField] private GameObject kickHitboxObject;

    private PlayerHitbox punchHitbox;
    private PlayerHitbox kickHitbox;
    // ------------------------------------

    // NOWE: Pola do Stunu i Cooldownu
    [Header("Stun i Cooldown")]
    [Tooltip("Czy gracz jest aktualnie oszołomiony (blokada ruchu).")]
    public bool isStunned = false;
    private float stunCooldownEndTime = 0f; // Czas, po którym nauczyciel może gonić gracza ponownie

    // NOWE: Pola prędkości
    public float currentAgentSpeed; // Aktualnie obliczona prędkość NavMeshAgenta
    private float currentMoveSpeed; // Aktualnie obliczona prędkość (docelowa dla NavMeshAgent)
    private Vector3 moveInput;
    private Vector2 lookInput;

    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private Animator animator;
    private bool isGrounded = true;

    // Nowe stany dla ruchu i akcji
    public bool isRunning = false;
    public bool isSneaking = false;
    public bool isActionLocked = false;

    public bool isCrouch;
    public bool isMoveObject;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    public bool canRotate = true;
    public bool canMoveObjects;
    public InteractorController tempInteractorController;
    private Quaternion lastIntendedRotation;
    public bool hasKey;
    public bool isMan;
    public CapsuleCollider capsuleCollider;

    // Pola do dźwięku
    public AudioClip gosiaHop;
    public AudioClip jasHit;
    public AudioClip gosiaHit;
    public AudioClip JasWalk;

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
        if (agent != null)
        {
            agent.speed = baseMoveSpeed;
            agent.updateRotation = false;
            agent.acceleration = accelerationRate;
        }
        currentMoveSpeed = baseMoveSpeed;

        if (playerMesh != null)
        {
            playerRenderer = playerMesh.GetComponentInChildren<Renderer>();
        }
        punchHitboxObject.transform.SetParent(playerMesh.transform);
        kickHitboxObject.transform.SetParent(playerMesh.transform);
        // --- INICJALIZACJA HITBOXÓW ---
        if (punchHitboxObject != null)
        {
            punchHitbox = punchHitboxObject.GetComponent<PlayerHitbox>();
            if (punchHitbox != null) punchHitbox.ownerController = this; // Ustawienie właściciela
            punchHitboxObject.SetActive(false); // Domyślnie wyłączony
           
        }

        if (kickHitboxObject != null)
        {
            kickHitbox = kickHitboxObject.GetComponent<PlayerHitbox>();
            if (kickHitbox != null) kickHitbox.ownerController = this; // Ustawienie właściciela
            kickHitboxObject.SetActive(false); // Domyślnie wyłączony
        }
        // ------------------------------
    }

    /// <summary>
    /// Sprawdza, czy gracz jest w cooldownie i nie może być ponownie złapany/ścignany przez nauczyciela.
    /// </summary>
    public bool IsInCooldown()
    {
        return Time.time < stunCooldownEndTime;
    }

    /// <summary>
    /// Ustawia gracza w stan stun (z mruganiem) i ustawia cooldown nauczyciela.
    /// </summary>
    /// <param name="stunDuration">Czas trwania stunu (mrugania).</param>
    /// <param name="chaseCooldown">Czas, przez który nauczyciel nie może ponownie gonić tego gracza.</param>
    public void SetStunned(float stunDuration, float chaseCooldown)
    {
        if (isStunned) return;

        isStunned = true;
        isActionLocked = true;

        // ZABLOKOWANIE RUCHU AGENTA
        if (agent != null)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }

        // Ustawienie końca cooldownu dla nauczyciela (łączny czas)
        stunCooldownEndTime = Time.time + stunDuration + chaseCooldown;

        StartCoroutine(StunCoroutine(stunDuration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        float startTime = Time.time;
        float blinkInterval = 0.2f;

        while (Time.time < startTime + duration)
        {
            if (playerRenderer != null)
            {
                playerRenderer.enabled = !playerRenderer.enabled; // Mruganie
            }
            yield return new WaitForSeconds(blinkInterval);
        }

        // Koniec stunu (fizycznego)
        if (playerRenderer != null)
        {
            playerRenderer.enabled = true;
        }

        isStunned = false;
        isActionLocked = false;
        if (agent != null)
        {
            agent.isStopped = false;
        }

        Debug.Log($"Gracz {playerID} jest wolny. Nauczyciel ma cooldown do {stunCooldownEndTime}s.");
    }

    private void Update()
    {
        // 🛑 ZABEZPIECZENIE PRZED NullReferenceException (InputController.Instance)
        if (InputController.Instance == null)
        {
            // Możesz zmienić na Debug.LogWarning jeśli to normalne, że jest ładowany później
            Debug.LogError("BŁĄD: InputController.Instance nie jest dostępny! Sprawdź kolejność wykonania skryptów.");
            return;
        }
        // ----------------------------------------------------------------------

        Vector2 rawInput = Vector2.zero;
        float maxSpeed;

        // --- Blokada akcji/ruchu ---
        if (isActionLocked || isStunned)
        {
            moveInput = Vector3.zero;
            if (agent != null) agent.velocity = Vector3.zero;
            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsRunning", false);
            }
            return;
        }

        // --- 1. Pobranie surowego wejścia ---
        if (playerID == 1)
        {
            rawInput = InputController.Instance.player1MoveValue;
            lookInput = InputController.Instance.Player1Actions.lookAction.Value;

            if (agent != null && agent.enabled && isGrounded && canJump && InputController.Instance.Player1Actions.jumpAction.WasPressed) Jump();
            if (InputController.Instance.Player1Actions.interactionAction.WasPressed && tempInteractorController != null) Interact();
            if (InputController.Instance.Player1Actions.crowlAction.WasPressed) Crouch();

            isRunning = InputController.Instance.Player1Actions.runAction.IsPressed;
            isSneaking = InputController.Instance.Player1Actions.sneakAction.IsPressed;
            if (InputController.Instance.Player1Actions.punchAction.WasPressed) Punch();
            if (InputController.Instance.Player1Actions.kickAction.WasPressed) Kick();
        }
        else if (playerID == 2)
        {
            rawInput = InputController.Instance.player2MoveValue;
            lookInput = InputController.Instance.Player2Actions.lookAction.Value;

            if (agent != null && agent.enabled && isGrounded && canJump && InputController.Instance.Player2Actions.jumpAction.WasPressed) Jump();
            if (InputController.Instance.Player2Actions.interactionAction.WasPressed && tempInteractorController != null) Interact();
            if (InputController.Instance.Player2Actions.crowlAction.WasPressed) Crouch();

            isRunning = InputController.Instance.Player2Actions.runAction.IsPressed;
            isSneaking = InputController.Instance.Player2Actions.sneakAction.IsPressed;
            if (InputController.Instance.Player2Actions.punchAction.WasPressed) Punch();
            if (InputController.Instance.Player2Actions.kickAction.WasPressed) Kick();
        }

        // --- 2. Konwersja Vector2 na Vector Świata ---
        float rawMagnitude = rawInput.magnitude;

        if (rawMagnitude > 0.1f)
        {
            if (playerCameraTransform != null)
            {
                Vector3 forward = playerCameraTransform.forward;
                Vector3 right = playerCameraTransform.right;

                forward.y = 0;
                right.y = 0;

                forward.Normalize();
                right.Normalize();

                Vector3 adjustedMovement = (forward * rawInput.y + right * rawInput.x);

                if (adjustedMovement.sqrMagnitude > 1f)
                {
                    adjustedMovement.Normalize();
                }
                moveInput = adjustedMovement;
            }
            else
            {
                moveInput = new Vector3(rawInput.x, 0, rawInput.y);
                Debug.LogWarning("Brak przypisanego playerCameraTransform! Ruch działa w trybie World-Space. Proszę przypisać kamerę.");
            }
        }
        else
        {
            moveInput = Vector3.zero;
        }

        // --- 3. Animacje i Prędkość (Logika FIZYCZNA) ---

        // Logika ustawiania maksymalnej prędkości i stanów bool w Animatorze
        if (isCrouch)
        {
            maxSpeed = sneakSpeed;
            if (animator != null)
            {
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsSneaking", true);
                animator.SetBool("isCrouch", true);
            }
        }
        else if (isRunning && !isSneaking)
        {
            maxSpeed = runSpeed;
            if (animator != null)
            {
                animator.SetBool("IsRunning", true);
                animator.SetBool("IsSneaking", false);
                animator.SetBool("isCrouch", false);
            }
        }
        else if (isSneaking && !isRunning)
        {
            maxSpeed = sneakSpeed;
            if (animator != null)
            {
                animator.SetBool("IsSneaking", true);
                animator.SetBool("IsRunning", false);
                animator.SetBool("isCrouch", false);
            }
        }
        else
        {
            maxSpeed = baseMoveSpeed;
            if (animator != null)
            {
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsSneaking", false);
                animator.SetBool("isCrouch", false);
            }
        }

        // --- Aktualizacja NavMeshAgent Speed (Docelowa prędkość fizyczna) ---
        float targetAgentSpeed = maxSpeed * rawMagnitude;
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetAgentSpeed, Time.deltaTime * accelerationRate);

        if (agent != null)
        {
            agent.speed = currentMoveSpeed;
        }

        // --- Aktualizacja Animator SpeedBlend (NAPRAWIONA WERSJA) ---
        if (animator != null && agent != null)
        {
            // POBIERZ FAKTYCZNĄ PRĘDKOŚĆ AGENTA
            currentAgentSpeed = agent.velocity.magnitude;

            // OBLICZENIE ZNORMALIZOWANEJ PRĘDKOŚCI (0.0 do 1.0)
            float currentMaxSpeed = isCrouch ? sneakSpeed : (isRunning ? runSpeed : baseMoveSpeed);

            // Znormalizowanie: Faktyczna prędkość / Maksymalna prędkość
            float targetSpeedBlend = (currentMaxSpeed > 0) ? Mathf.Clamp01(currentAgentSpeed / currentMaxSpeed) : 0f;

            // Wygładzanie blendu dla płynniejszych przejść
            float currentBlend = animator.GetFloat("SpeedBlend");
            float blendedValue = GetComponent<NavMeshAgent>().speed;

            animator.SetFloat("SpeedBlend", blendedValue);

            // Ustawienie IsMoving: jeśli faktyczna prędkość jest większa niż minimalna
            if (currentAgentSpeed > 0.001f)
            {
                animator.SetBool("IsMoving", true);
            }
            else
            {
                animator.SetBool("IsMoving", false);
            }
        }
    }

    private void FixedUpdate()
    {
        // Ruch i Rotacja NavMeshAgent
        if (agent != null && agent.enabled && !isStunned)
        {
            Vector3 worldMovement = moveInput;

            if (worldMovement.magnitude > 0.01f)
            {
                // --- ROTACJA ---
                if (canRotate && playerMesh != null)
                {
                    Vector3 intendedDirection = lookInput.magnitude > 0.1f
                        ? new Vector3(lookInput.x, 0, lookInput.y)
                        : moveInput;

                    Vector3 direction = Vector3.ProjectOnPlane(intendedDirection.normalized, Vector3.up);

                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    lastIntendedRotation = targetRotation;

                    playerMesh.transform.rotation = Quaternion.Slerp(
                        playerMesh.transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.fixedDeltaTime
                    );
                }

                // --- RUCH ---
                Vector3 desiredMovement = worldMovement * agent.speed * Time.fixedDeltaTime;
                agent.Move(desiredMovement);
            }
            else
            {
                agent.velocity = Vector3.zero;
            }
        }
        else if (agent != null && isStunned)
        {
            // Zablokuj ruch i obrót NavMeshAgenta podczas stunu
            agent.velocity = Vector3.zero;
        }
    }

    // --- Reszta Metod (Crouch, Jump, Interact, GetHit, WalkSound, itd.) ---

    private void Crouch()
    {
        if (!canCrouch) return;

        if (isCrouch)
        {
            // Wychodzenie z kucania
            isCrouch = false;
            capsuleCollider.height = 1;
            capsuleCollider.DOComplete();
            DOTween.To(() => capsuleCollider.center, x => capsuleCollider.center = x, new Vector3(0, 0f, 0), 0.5f);
            if (agent != null) agent.height = 1.8f;

            OnActionFinished.Invoke();
        }
        else
        {
            // Kucanie
            isCrouch = true;
            capsuleCollider.height = 0.3f;
            capsuleCollider.DOComplete();
            DOTween.To(() => capsuleCollider.center, x => capsuleCollider.center = x, new Vector3(0, -0.35f, 0), 0.2f);
            if (agent != null) agent.height = 0.3f;

            OnActionStarted.Invoke();
        }
    }

    public void Interact()
    {
        if (tempInteractorController != null)
        {
            tempInteractorController.OnInteract(this);
        }
    }

    private IEnumerator PerformJump()
    {
        if (agent == null) yield break;

        float jumpDuration = 0.5f;
        Vector3 jumpDirection = moveInput.normalized * 0.1f;

        DOTween.Sequence()
            .Append(transform.DOJump(transform.position + jumpDirection, jumpForce * 0.1f, 1, jumpDuration).SetEase(Ease.OutQuad))
            .OnComplete(() =>
            {
                agent.enabled = true;
            });

        yield break;
    }

    private void Jump()
    {
        if (canJump && agent != null && agent.enabled)
        {
            GetComponent<AudioSource>().PlayOneShot(gosiaHop);
            agent.enabled = false;
            StartCoroutine(PerformJump());
        }
    }

    // --- ZMODYFIKOWANE Metody walki (włączanie/wyłączanie Hitboxa) ---
    private void Punch()
    {
        if (isActionLocked) return;

        isActionLocked = true;
        if (canRotate && playerMesh != null && lastIntendedRotation != Quaternion.identity)
        {
            playerMesh.transform.rotation = lastIntendedRotation;
        }

        if (animator != null) animator.SetTrigger("Punch");

        // NOWE: Włączenie Hitboxa i ustawienie obrażeń punktowych
        if (punchHitbox != null && punchHitboxObject != null)
        {
            punchHitbox.SetScoreDamage(5); // Ustaw, że Punch odejmuje 5 punktów
            punchHitbox.ResetHitbox();
            punchHitboxObject.SetActive(true);
        }

        StartCoroutine(ActionLockoutCoroutine(punchDuration));
        OnActionStarted.Invoke();
    }

    private void Kick()
    {
        if (isActionLocked) return;

        isActionLocked = true;
        if (canRotate && playerMesh != null && lastIntendedRotation != Quaternion.identity)
        {
            playerMesh.transform.rotation = lastIntendedRotation;
        }

        if (animator != null) animator.SetTrigger("Kick");

        // NOWE: Włączenie Hitboxa i ustawienie obrażeń punktowych
        if (kickHitbox != null && kickHitboxObject != null)
        {
            kickHitbox.SetScoreDamage(15); // Ustaw, że Kick odejmuje 15 punktów
            kickHitbox.ResetHitbox();
            kickHitboxObject.SetActive(true);
        }

        StartCoroutine(ActionLockoutCoroutine(kickDuration));
        OnActionStarted.Invoke();
    }

    private IEnumerator ActionLockoutCoroutine(float duration)
    {
        if (agent != null && agent.enabled)
        {
            agent.velocity = Vector3.zero;
        }

        yield return new WaitForSeconds(duration);

        // NOWE: Wyłączenie Hitboxa po zakończeniu akcji
        if (punchHitboxObject != null && punchHitboxObject.activeSelf)
        {
            punchHitboxObject.SetActive(false);
        }
        if (kickHitboxObject != null && kickHitboxObject.activeSelf)
        {
            kickHitboxObject.SetActive(false);
        }

        isActionLocked = false;
        OnActionFinished.Invoke();
    }


    public void SnapRotationToEnd()
    {
        if (lastIntendedRotation != Quaternion.identity && playerMesh != null)
        {
            playerMesh.transform.rotation = Quaternion.RotateTowards(playerMesh.transform.rotation, lastIntendedRotation, rotationSpeed * 2 * Time.fixedDeltaTime);
        }
    }

    public void Movable(bool valuemove)
    {
        isMoveObject = valuemove;
        if (animator != null)
        {
            animator.SetBool("isMovable", valuemove);
        }
    }

    public void GetHit()
    {
        if (playerID == 1)
        {
            InputController.Instance.Vibrate(0.25f, InputController.Instance.Player1Actions, 0.3f);
        }

        if (playerID == 2)
        {
            InputController.Instance.Vibrate(0.25f, InputController.Instance.Player2Actions, 0.3f);
        }

        if (isMan) GetComponent<AudioSource>().PlayOneShot(jasHit);
        else GetComponent<AudioSource>().PlayOneShot(gosiaHit);
    }

    public void WalkSound()
    {
        if (agent != null && agent.velocity.magnitude > 0.1f)
        {
            var actions = playerID == 1 ? InputController.Instance.Player1Actions : InputController.Instance.Player2Actions;
            InputController.Instance.Vibrate(0.1f, actions, 0.1f);

            if (isMan) GetComponent<AudioSource>().PlayOneShot(JasWalk);
        }
    }
}