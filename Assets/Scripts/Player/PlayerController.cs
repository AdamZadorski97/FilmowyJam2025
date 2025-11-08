using DG.Tweening;
using InControl;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // Wymagane dla NavMeshAgent
using XInputDotNetPure;
using static UnityEngine.GraphicsBuffer;

public class PlayerController : MonoBehaviour
{
    public InputDevice InputDevice { get; set; }

    public int playerID;
    // Usunięto Rigidbody, dodano NavMeshAgent
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpForce = 5f;

    private Vector3 moveInput;
    private Vector2 lookInput;

    // ... (inne pola pozostają bez zmian)
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask wallMask; // Używamy wallMask, ale NavMesh Agent powinien unikać ścian
    [SerializeField] private Animator animator;
    private bool isGrounded;
    public bool isCrouch;
    public bool isMoveObject;
    [SerializeField] private bool canJump = true; // Domyślnie można skakać
    [SerializeField] private bool canCrouch = true; // Domyślnie można kucać
    public bool canRotate = true; // Z NavMeshAgent rotacja jest często automatyczna
    public bool canMoveObjects;
    public InteractorController tempInteractorController;
    private bool interact;
    private Quaternion lastIntendedRotation;
    public bool hasKey;
    public bool isMan;
    public CapsuleCollider capsuleCollider;
    public Vector2 test1;
    public Vector2 test2;
    // Nowe pole, jeśli NavMeshAgent nie jest wpięty w Inspector
    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
        // Ustawienie początkowej prędkości agenta
        if (agent != null)
        {
            agent.speed = moveSpeed;
            // NavMesh Agent jest odpowiedzialny za ruch, więc wyłączamy jego automatyczną rotację, 
            // jeśli chcemy kontrolować ją sami (jak w oryginalnym kodzie)
            // agent.updateRotation = false; // Odkomentuj, jeśli chcesz ręcznie kontrolować rotację
        }
    }

    private void Update()
    {
        // Sprawdzenie uziemienia nie jest tak kluczowe, gdy używamy NavMeshAgent, 
        // ale może być przydatne do animacji lub skoków.
        // Jeśli agent.isOnNavMesh jest false, postać może spaść, ale NavMesh Agent
        // nie ma wbudowanego wykrywania "upadku".
        // W NavMeshAgent lepszym zamiennikiem isGrounded jest agent.isStopped lub agent.remainingDistance < agent.stoppingDistance.
        // Dla skoku:
        test1 = InputController.Instance.player1MoveValue;
        test2 = InputController.Instance.player2MoveValue;
        // Pobieranie danych wejściowych (bez zmian)
        if (playerID == 1)
        {
            Vector3 input = InputController.Instance.player1MoveValue;
            moveInput = new Vector3(input.x, input.y, input.z); // Nie normalizujemy od razu, by zachować siłę
            lookInput = InputController.Instance.Player1Actions.lookAction.Value;
            if (isGrounded && InputController.Instance.Player1Actions.jumpAction.WasPressed)
            {
                Jump();
            }
            if (InputController.Instance.Player1Actions.interactionAction.WasPressed && tempInteractorController != null)
            {
                tempInteractorController.OnInteract(this);
            }
            if (InputController.Instance.Player1Actions.crowlAction.WasPressed)
            {
                Crouch();
            }
        }
        else if (playerID == 2)
        {
            Vector3 input = InputController.Instance.player2MoveValue;
            moveInput = new Vector3(input.x, input.y, input.z); // Nie normalizujemy od razu, by zachować siłę
            lookInput = InputController.Instance.Player2Actions.lookAction.Value;
            if (isGrounded && InputController.Instance.Player2Actions.jumpAction.WasPressed)
            {
                Jump();
            }
            if (InputController.Instance.Player2Actions.interactionAction.WasPressed && tempInteractorController != null)
            {
                tempInteractorController.OnInteract(this);
            }
            if (InputController.Instance.Player2Actions.crowlAction.WasPressed)
            {
                Crouch();
            }
        }

        // Kontrola animacji
        // Używamy wejścia wejściowego, aby animacja reagowała natychmiast
        if (moveInput.magnitude > 0.1f)
        {
          //  animator.SetBool("IsRunning", true);
        }
        else
        {
          //  animator.SetBool("IsRunning", false);
        }

        // Zaktualizuj prędkość agenta na podstawie stanu kucania
        if (agent != null)
        {
            agent.speed = isCrouch ? moveSpeed / 2 : moveSpeed;
        }
    }

    private void FixedUpdate()
    {
        // Ruch NavMeshAgent
        if (agent != null && agent.enabled)
        {
            Vector3 movement =  new Vector3( moveInput.x, moveInput.z, moveInput.y);

                    Vector3 worldMovement = movement * agent.speed * Time.fixedDeltaTime;
                    agent.Move(worldMovement);
                    Debug.Log(worldMovement);
        }


    }

    // Zmieniono, aby działało na transform.rotation, a nie rb.rotation
    public void SnapRotationToEnd()
    {
        if (lastIntendedRotation != Quaternion.identity)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lastIntendedRotation, rotationSpeed * 2 * Time.fixedDeltaTime);
        }
    }

    public void SnapToMovable()
    {
        // Logika pozostaje pusta, jeśli nie jest potrzebna
    }

    public AudioClip gosiaHop;
    private void Jump()
    {
        // Skok z NavMesh Agent jest bardziej skomplikowany, bo Agent "oczekuje" bycia na siatce.
        // Jeśli chcemy skakać, musimy tymczasowo wyłączyć NavMesh Agent i użyć Rigidbody
        // (jeśli postać go ma, a ty usunąłeś), lub dodać sztuczną siłę w górę
        // i po lądowaniu włączyć go ponownie.
        // W tej wersji, użyję prostej sztuczki, która działa, jeśli agent jest wyłączony lub
        // po prostu ignorujemy go na krótki czas. Użyjemy agent.velocity.

        if (canJump && agent != null && agent.enabled)
        {
            GetComponent<AudioSource>().PlayOneShot(gosiaHop);
            agent.enabled = false;
        }
    }


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
                // Zmiana wysokości agenta po kucaniu
                if (agent != null) agent.height = 1.8f; // Standardowa wysokość
            }
            else
            {
                isCrouch = true;
                animator.SetBool("isCrouch", true);
                capsuleCollider.height = 0.3f;
                capsuleCollider.DOComplete();
                DOTween.To(() => capsuleCollider.center, x => capsuleCollider.center = x, new Vector3(0, -0.35f, 0), 0.2f);
                // Zmiana wysokości agenta na mniejszą
                if (agent != null) agent.height = 0.3f; // Wysokość kucania
            }
        }
    }

    // ... (pozostałe metody bez zmian, o ile nie używają Rigidbody)
    public void Movable(bool valuemove)
    {
        isMoveObject = valuemove;
        animator.SetBool("isMovable", valuemove);
    }

    public void Interact()
    {
        tempInteractorController.OnInteract(this);
    }

    public AudioClip jasHit;
    public AudioClip gosiaHit;
    public AudioClip JasWalk;

    public void GetHit()
    {
        // ... (Logika GetHit)
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
        // ... (Logika WalkSound)
        InputController.Instance.Vibrate(0.1f, InputController.Instance.Player1Actions, 0.1f);
        if (isMan) GetComponent<AudioSource>().PlayOneShot(JasWalk);
    }

}