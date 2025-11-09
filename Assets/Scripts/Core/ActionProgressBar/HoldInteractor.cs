using UnityEngine;
using UnityEngine.Events;

public class HoldInteractor : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    [SerializeField]
    private float maxHoldTime = 2.0f;

    // --- Zmiana 2: Dodanie publicznej zmiennej/właściwości stanu trzymania (0 do 1) ---
    // Używamy GetHoldProgress() jako implementacji dla nowej właściwości.
    /// <summary>
    /// Zwraca aktualny postęp trzymania jako wartość od 0.0 do 1.0 (alias dla GetHoldProgress()).
    /// </summary>
    public float HoldProgress
    {
        get
        {
            return GetHoldProgress();
        }
    }
    // -----------------------------------------------------------------------------------

    [Header("Eventy")]
    // --- Zmiana 1: Dodanie eventu startu przytrzymania ---
    [Tooltip("Wywoływane, gdy gracz ZACZYNA przytrzymywać przycisk.")]
    public UnityEvent OnHoldStart;
    // -------------------------------------------------------

    [Tooltip("Wywoływane, gdy gracz PRAWIDŁOWO przytrzyma przycisk przez 'maxHoldTime'")]
    public UnityEvent OnHoldComplete;

    [Space]

    [Header("Second Interaction")]

    public bool SecondInteraction = false;

    // --- Zmiana 1: Dodanie eventu startu przytrzymania ---
    [Tooltip("Wywoływane, gdy gracz ZACZYNA przytrzymywać przycisk.")]
    public UnityEvent OnHoldStart2;
    // -------------------------------------------------------

    [Tooltip("Wywoływane, gdy gracz PRAWIDŁOWO przytrzyma przycisk przez 'maxHoldTime'")]
    public UnityEvent OnHoldComplete2;

    // --- Zmienne prywatne ---
    private PlayerController currentPlayerInTrigger;
    private float currentHoldTime = 0f;
    private bool isHolding = false;
    private bool holdWasCompleted = false;

    // SECONDARY INTERACTION VARIABLES
    private float currentHoldTime2 = 0f;
    private bool isHolding2 = false;
    private bool holdWasCompleted2 = false;

    // --- DEBUG ---
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private void Log(string message)
    {
        return;
        if (enableDebugLogs)
        {
            Debug.Log($"[HoldInteractor - {gameObject.name}] {message}");
        }
    }
    // --- KONIEC DEBUG ---

    private void Start()
    {
        maxHoldTime += Random.Range(0, 10) * 0.01f;
    }


    // --- Metoda GetHoldProgress() została przywrócona zgodnie z Twoją prośbą ---
    public float GetHoldProgress()
    {
        if (maxHoldTime <= 0) return 0;
        return Mathf.Clamp01(currentHoldTime / maxHoldTime);
    }

    public float GetHoldProgress2()
    {
        if (maxHoldTime <= 0) return 0;
        return Mathf.Clamp01(currentHoldTime2 / maxHoldTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null || currentPlayerInTrigger != null) return;

        Log($"Gracz {player.name} (ID: {player.playerID}) wszedł w trigger. Resetuję stan.");
        currentPlayerInTrigger = player;
        currentHoldTime = 0f;
        isHolding = false;
        holdWasCompleted = false;

        // Reset stanu Secondary
        currentHoldTime2 = 0f;
        isHolding2 = false;
        holdWasCompleted2 = false;

    }

    private void OnTriggerExit(Collider other)
    {
        Log($"OnTriggerExit z obiektem: {other.name}");
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null && player == currentPlayerInTrigger)
        {
            Log($"Gracz {player.name}, który był śledzony, opuścił trigger.");

            if (isHolding && !holdWasCompleted)
            {
                Log("...Gracz trzymał przycisk, ale wyszedł. ANULOWANIE.");

                // Użycie statycznego serwisu (który przekieruje do Singletona)
                HoldProgressService.Hide(currentPlayerInTrigger.playerID);
                currentPlayerInTrigger.OnActionFinished.Invoke(); // WYZWALANIE ZAKOŃCZENIA
            }

            // Anulowanie Secondary
            if (isHolding2 && !holdWasCompleted2)
            {
                Log("...Secondary: Gracz trzymał, ale wyszedł. ANULOWANIE.");
                // Możesz potrzebować HoldProgressService.Hide(ID, 'Secondary') jeśli używasz drugiego paska
                currentPlayerInTrigger.OnActionFinished.Invoke();
            }

            Log("...Czyszczę referencję do gracza.");
            currentPlayerInTrigger = null;
            currentHoldTime = 0f;
            isHolding = false;
            holdWasCompleted = false;
            currentHoldTime2 = 0f;
            isHolding2 = false;
            holdWasCompleted2 = false;
        }
    }

    private void Update()
    {
        if (currentPlayerInTrigger == null) return;

        int currentID = currentPlayerInTrigger.playerID;
        bool isInteractPressed = false;
        bool isSecondInteractPressed = false; // Nowa zmienna dla drugiej interakcji

        // -----------------------------------------------------------------------------------
        // 1. ODCZYT WEJŚCIA
        // -----------------------------------------------------------------------------------

        // Odczyt wejścia Primary Action (niezmieniony)
        if (currentID == 1)
        {
            isInteractPressed = InputController.Instance.Player1Actions.interactionAction.IsPressed;
            if (SecondInteraction)
            {
                // Załóżmy, że Player1Actions ma secondaryAction
                isSecondInteractPressed = InputController.Instance.Player1Actions.jumpAction.IsPressed;
            }
        }
        else if (currentID == 2)
        {
            isInteractPressed = InputController.Instance.Player2Actions.interactionAction.IsPressed;
            if (SecondInteraction)
            {
                // Załóżmy, że Player2Actions ma secondaryAction
                isSecondInteractPressed = InputController.Instance.Player2Actions.jumpAction.IsPressed;
            }
        }

        // -----------------------------------------------------------------------------------
        // 2. LOGIKA PRIMARY INTERACTION (oryginalna)
        // -----------------------------------------------------------------------------------

        if (!holdWasCompleted) // Kontynuujemy tylko jeśli primary nie jest ukończona
        {
            if (isInteractPressed)
            {
                if (!isHolding)
                {
                    Log("Gracz ZACZĄŁ trzymać przycisk (Primary).");
                    isHolding = true;
                    OnHoldStart.Invoke();
                    HoldProgressService.Show(this, currentID);
                    currentPlayerInTrigger.OnActionStarted.Invoke();
                }

                currentHoldTime += Time.deltaTime;

                if (currentHoldTime >= maxHoldTime)
                {
                    ScoreService.AddPoints(currentPlayerInTrigger.playerID, 10);
                    Log("SUKCES! Osiągnięto maxHoldTime (Primary).");
                    OnHoldComplete.Invoke();
                    HoldProgressService.Hide(currentID);
                    currentPlayerInTrigger.OnActionFinished.Invoke();
                    holdWasCompleted = true;
                }
            }
            else
            {
                if (isHolding)
                {
                    Log("Gracz PUŚCIŁ przycisk (Primary) przed czasem. ANULOWANIE.");
                    HoldProgressService.Hide(currentID);
                    currentPlayerInTrigger.OnActionFinished.Invoke();
                    isHolding = false;
                    currentHoldTime = 0f;
                }
            }
        } // Koniec bloku Primary Interaction

        // -----------------------------------------------------------------------------------
        // 3. LOGIKA SECOND INTERACTION (NOWA, warunkowa)
        // -----------------------------------------------------------------------------------

        if (SecondInteraction && !holdWasCompleted2) // Kontynuujemy tylko jeśli SecondInteraction jest TRUE i nieukończona
        {
            if (isSecondInteractPressed)
            {
                if (!isHolding2)
                {
                    Log("Gracz ZACZĄŁ trzymać przycisk (Secondary).");
                    isHolding2 = true;
                    OnHoldStart2.Invoke();

                    // UWAGA: Jeśli SecondInteraction ma mieć swój własny pasek postępu, 
                    // musisz dostosować HoldProgressService.Show, aby obsługiwał dwie akcje.
                    // Na razie używamy tylko logiki.
                    // HoldProgressService.Show(this, currentID, "Secondary"); 
                    currentPlayerInTrigger.OnActionStarted.Invoke();
                }

                currentHoldTime2 += Time.deltaTime;

                if (currentHoldTime2 >= maxHoldTime)
                {
                    ScoreService.AddPoints(currentPlayerInTrigger.playerID, 20); // Dajmy inne punkty dla drugiej akcji
                    Log("SUKCES! Osiągnięto maxHoldTime (Secondary).");
                    OnHoldComplete2.Invoke();

                    // HoldProgressService.Hide(currentID, "Secondary");
                    currentPlayerInTrigger.OnActionFinished.Invoke();
                    holdWasCompleted2 = true;
                }
            }
            else
            {
                if (isHolding2)
                {
                    Log("Gracz PUŚCIŁ przycisk (Secondary) przed czasem. ANULOWANIE.");

                    // HoldProgressService.Hide(currentID, "Secondary");
                    currentPlayerInTrigger.OnActionFinished.Invoke();
                    isHolding2 = false;
                    currentHoldTime2 = 0f;
                }
            }
        } // Koniec bloku Second Interaction
    }
}