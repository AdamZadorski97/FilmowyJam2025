using DG.Tweening;
using InControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using XInputDotNetPure;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviour
{
    public InputDevice InputDevice { get; set; }

    public int playerID;

    [SerializeField] private NavMeshAgent agent;

    // !!! NOWA ZMIENNA: Mesh/Wizualny Model Postaci !!!
    [SerializeField] private GameObject playerMesh;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f; // Szybkość obrotu
    [SerializeField] private float jumpForce = 5f;

    private Vector3 moveInput;
    private Vector2 lookInput;

    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private Animator animator;
    private bool isGrounded;
    public bool isCrouch;
    public bool isMoveObject;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    public bool canRotate = true;
    public bool canMoveObjects;
    public InteractorController tempInteractorController;
    private bool interact;
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
            // Kluczowa zmiana: Wyłączamy automatyczną rotację NavMeshAgent 
            // Kontrolujemy rotację na obiekcie playerMesh
            agent.updateRotation = false;
        }
    }

    private void Update()
    {
        // --- Obsługa Wejścia (Input) ---
        if (playerID == 1)
        {
            Vector3 input = InputController.Instance.player1MoveValue;
            moveInput = new Vector3(input.x, input.y, input.z);
            lookInput = InputController.Instance.Player1Actions.lookAction.Value;

            if (agent != null && agent.enabled && isGrounded && canJump && InputController.Instance.Player1Actions.jumpAction.WasPressed)
            {
                Jump();
            }
            if (InputController.Instance.Player1Actions.interactionAction.WasPressed && tempInteractorController != null)
            {
                tempInteractorController.OnInteract(this);
            }
            // ... (Crowl Action)
        }
        else if (playerID == 2)
        {
            Vector3 input = InputController.Instance.player2MoveValue;
            moveInput = new Vector3(input.x, input.y, input.z);
            lookInput = InputController.Instance.Player2Actions.lookAction.Value;

            if (agent != null && agent.enabled && isGrounded && canJump && InputController.Instance.Player2Actions.jumpAction.WasPressed)
            {
                Jump();
            }
            if (InputController.Instance.Player2Actions.interactionAction.WasPressed && tempInteractorController != null)
            {
                tempInteractorController.OnInteract(this);
            }
            // ... (Crowl Action)
        }

        // --- Animacje i Prędkość ---
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
            // Mapowanie wejścia 2D/kontrolera (X, Y) na wektor świata 3D (X, 0, Z)
            Vector3 rawMovement = new Vector3(moveInput.x, 0f, moveInput.y);

            // Tylko jeśli jest ruch
            if (rawMovement.magnitude > 0.01f)
            {
                // --- ROTACJA (Najważniejsza zmiana: rotujemy tylko playerMesh) ---
                if (canRotate && playerMesh != null)
                {
                    // Wektor kierunku rzutowany na płaszczyznę Y=0 (ruch horyzontalny)
                    Vector3 direction = Vector3.ProjectOnPlane(rawMovement.normalized, Vector3.up);

                    // 1. Oblicz docelową rotację
                    Quaternion targetRotation = Quaternion.LookRotation(direction);

                    // 2. Płynnie obracaj TYLKO MESH
                    playerMesh.transform.rotation = Quaternion.Slerp(
                        playerMesh.transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.fixedDeltaTime
                    );
                }

                // --- RUCH ---
                Vector3 desiredMovement = rawMovement.normalized * agent.speed * Time.fixedDeltaTime;
                agent.Move(desiredMovement);
            }
            else
            {
                agent.velocity = Vector3.zero;
            }
        }
    }

    // --- Reszta Metod (Jump, Movable, Interact, GetHit, WalkSound) pozostaje bez zmian ---

    public void SnapRotationToEnd()
    {
        if (lastIntendedRotation != Quaternion.identity && playerMesh != null)
        {
            playerMesh.transform.rotation = Quaternion.RotateTowards(playerMesh.transform.rotation, lastIntendedRotation, rotationSpeed * 2 * Time.fixedDeltaTime);
        }
    }

    private void Jump()
    {
        if (canJump && agent != null && agent.enabled)
        {
            GetComponent<AudioSource>().PlayOneShot(gosiaHop);
            agent.enabled = false;
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

    public void Interact()
    {
        if (tempInteractorController != null)
        {
            tempInteractorController.OnInteract(this);
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
            InputController.Instance.Vibrate(0.1f, InputController.Instance.Player1Actions, 0.1f);
            if (isMan) GetComponent<AudioSource>().PlayOneShot(JasWalk);
        }
    }
}