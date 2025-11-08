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

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;

    private Vector3 moveInput;
    private Vector2 lookInput;

    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private Animator animator;
    private bool isGrounded = true;
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
            agent.speed = moveSpeed;
            agent.updateRotation = false;
        }
    }

    private void Update()
    {
        Vector2 rawInput = Vector2.zero;

        // --- 1. Pobranie surowego wejścia ---
        if (playerID == 1)
        {
            rawInput = InputController.Instance.player1MoveValue;
            lookInput = InputController.Instance.Player1Actions.lookAction.Value;

            if (agent != null && agent.enabled && isGrounded && canJump && InputController.Instance.Player1Actions.jumpAction.WasPressed) Jump();
            if (InputController.Instance.Player1Actions.interactionAction.WasPressed && tempInteractorController != null) Interact();
            if (InputController.Instance.Player1Actions.crowlAction.WasPressed) Crouch();
        }
        else if (playerID == 2)
        {
            rawInput = InputController.Instance.player2MoveValue;
            lookInput = InputController.Instance.Player2Actions.lookAction.Value;

            if (agent != null && agent.enabled && isGrounded && canJump && InputController.Instance.Player2Actions.jumpAction.WasPressed) Jump();
            if (InputController.Instance.Player2Actions.interactionAction.WasPressed && tempInteractorController != null) Interact();
            if (InputController.Instance.Player2Actions.crowlAction.WasPressed) Crouch();
        }

        // --- 2. KLUCZOWA LOGIKA RUCHU: Konwersja Vector2 na Vector Świata ---
        if (rawInput.magnitude > 0.1f)
        {
            if (playerCameraTransform != null)
            {
                // LOGIKA Z ORIENTACJĄ KAMERY (preferowana)
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
                // FALLBACK: ORIENTACJA ŚWIATA (może być myląca, ale zapewnia ruch)
                // Wektor (X, 0, Z)
                moveInput = new Vector3(rawInput.x, 0, rawInput.y);
                Debug.LogWarning("Brak przypisanego playerCameraTransform! Ruch działa w trybie World-Space. Proszę przypisać kamerę.");
            }
        }
        else
        {
            moveInput = Vector3.zero;
        }


        // --- 3. Animacje i Prędkość ---
        if (animator != null)
        {
            if (moveInput.magnitude > 0.1f)
            {
                animator.SetBool("IsRunning", true);
            }
            else
            {
                animator.SetBool("IsRunning", false);
            }
        }

        if (agent != null)
        {
            agent.speed = isCrouch ? moveSpeed / 2f : moveSpeed;
        }
    }

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
                    // Używamy moveInput (skorygowanego) jako kierunku
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
        if (canCrouch)
        {
            if (isCrouch)
            {
                isCrouch = false;
                animator.SetBool("isCrouch", false);
                capsuleCollider.height = 1;
                capsuleCollider.DOComplete();
                DOTween.To(() => capsuleCollider.center, x => capsuleCollider.center = x, new Vector3(0, 0f, 0), 0.5f);
                if (agent != null) agent.height = 1.8f;

                OnActionFinished.Invoke();
            }
            else
            {
                isCrouch = true;
                animator.SetBool("isCrouch", true);
                capsuleCollider.height = 0.3f;
                capsuleCollider.DOComplete();
                DOTween.To(() => capsuleCollider.center, x => capsuleCollider.center = x, new Vector3(0, -0.35f, 0), 0.2f);
                if (agent != null) agent.height = 0.3f;

                OnActionStarted.Invoke();
            }
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