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

    private float currentMoveSpeed; // Aktualnie obliczona prędkość (docelowa dla NavMeshAgent)
    private float animatorSpeedBlend; // Prędkość animatora (bezpośrednio z NavMeshAgent)
    private Vector3 moveInput;
    private Vector2 lookInput;

    // USUNIĘTO: blendVelocity, animationDampTime

    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private Animator animator;
    private bool isGrounded = true;

    // Nowe stany dla ruchu i akcji
    public bool isRunning = false;
    public bool isSneaking = false;
    public bool isActionLocked = false; // Blokada ruchu/wejścia podczas ataku

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
    }

    private void Update()
    {
        Vector2 rawInput = Vector2.zero;
        float maxSpeed;
        if (moveInput == Vector3(0, 0, 0)) ;
        animator.SetFloat("SpeedBlend", GetComponent<NavMeshAgent>().speed);
        // --- Blokada akcji/ruchu ---
        if (isActionLocked)
        {
            moveInput = Vector3.zero;
            if (agent != null) agent.velocity = Vector3.zero;
            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
                
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

        // --- Aktualizacja Animator SpeedBlend (BEZPOŚREDNIE PRZYPISANIE VELOCITY) ---
        if (animator != null && agent != null)
        {
            // POBIERZ FAKTYCZNĄ PRĘDKOŚĆ AGENTA
            float actualVelocityMagnitude = agent.velocity.magnitude;

            // NORMALIZACJA: Faktyczna prędkość agenta / Maksymalna prędkość w obecnym stanie
            // Jeśli maxSpeed == 0, SpeedBlend = 0.
            float targetSpeedBlend = (maxSpeed > 0) ? Mathf.Clamp01(actualVelocityMagnitude / maxSpeed) : 0f;

            // Ustawienie IsMoving: jeśli faktyczna prędkość jest większa niż minimalna
            if (targetSpeedBlend > 0.001f) // Minimalny próg dla ruchu
            {
                animator.SetBool("IsMoving", true);
            }
            else
            {
                animator.SetBool("IsMoving", false);
            }

            // BEZPOŚREDNIE PRZYPISANIE - ZERO WYGŁADZANIA
          //  UpdateAnimatorSpeed(targetSpeedBlend, maxSpeed);
        }
    }

    // Zmieniona metoda: Usuwa SmoothDamp i blendVelocity


    private void FixedUpdate()
    {
        // Ruch i Rotacja NavMeshAgent
        if (agent != null && agent.enabled)
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
    }

    // --- Reszta Metod (Crouch, Jump, Interact, GetHit, WalkSound) ---

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

    // --- Metody walki ---
    private void Punch()
    {
        if (isActionLocked) return;

        isActionLocked = true;
        if (canRotate && playerMesh != null && lastIntendedRotation != Quaternion.identity)
        {
            playerMesh.transform.rotation = lastIntendedRotation;
        }

        if (animator != null) animator.SetTrigger("Punch");
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