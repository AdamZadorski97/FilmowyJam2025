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

    // --- Zmienne prywatne ---
    private PlayerController currentPlayerInTrigger;
    private float currentHoldTime = 0f;
    private bool isHolding = false;
    private bool holdWasCompleted = false;

    // --- DEBUG ---
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private void Log(string message)
    {
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

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null || currentPlayerInTrigger != null) return;

        Log($"Gracz {player.name} (ID: {player.playerID}) wszedł w trigger. Resetuję stan.");
        currentPlayerInTrigger = player;
        currentHoldTime = 0f;
        isHolding = false;
        holdWasCompleted = false;
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

            Log("...Czyszczę referencję do gracza.");
            currentPlayerInTrigger = null;
            currentHoldTime = 0f;
            isHolding = false;
            holdWasCompleted = false;
        }
    }

    private void Update()
    {
        if (currentPlayerInTrigger == null || holdWasCompleted) return;

        bool isInteractPressed = false;
        int currentID = currentPlayerInTrigger.playerID;

        // Zakładamy, że PlayerController i InputController są poprawnie zaimplementowane
        if (currentID == 1)
        {
            isInteractPressed = InputController.Instance.Player1Actions.interactionAction.IsPressed;
        }
        else if (currentID == 2)
        {
            isInteractPressed = InputController.Instance.Player2Actions.interactionAction.IsPressed;
        }

        // --- Logika Timera ---
        if (isInteractPressed)
        {
            if (!isHolding)
            {
                Log("Gracz ZACZĄŁ trzymać przycisk.");
                isHolding = true;

                // --- Zmiana 1: Wywołanie eventu OnHoldStart ---
                OnHoldStart.Invoke();
                // ---------------------------------------------

                // Użycie statycznego serwisu (który przekieruje do Singletona)
                HoldProgressService.Show(this, currentID);
                currentPlayerInTrigger.OnActionStarted.Invoke(); // WYZWALANIE ROZPOCZĘCIA
            }

            currentHoldTime += Time.deltaTime;

            if (currentHoldTime >= maxHoldTime && !holdWasCompleted)
            {
                ScoreService.AddPoints(currentPlayerInTrigger.playerID, 10);
                Log("SUKCES! Osiągnięto maxHoldTime.");

                OnHoldComplete.Invoke();
                // Użycie statycznego serwisu (który przekieruje do Singletona)
                HoldProgressService.Hide(currentID);

                currentPlayerInTrigger.OnActionFinished.Invoke(); // WYZWALANIE ZAKOŃCZENIA
                holdWasCompleted = true;
            }
        }
        else
        {
            // Gracz NIE trzyma przycisku
            if (isHolding)
            {
                Log("Gracz PUŚCIŁ przycisk przed czasem. ANULOWANIE.");

                // Użycie statycznego serwisu (który przekieruje do Singletona)
                HoldProgressService.Hide(currentID);
                currentPlayerInTrigger.OnActionFinished.Invoke(); // WYZWALANIE ZAKOŃCZENIA (anulowanie)

                isHolding = false;
                currentHoldTime = 0f;
            }
        }
    }
}