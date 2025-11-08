using UnityEngine;
using UnityEngine.Events;

public class HoldInteractor : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    [SerializeField]
    private float maxHoldTime = 2.0f;

    [Header("Eventy")]
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

                HoldProgressService.Hide();
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
        if (currentPlayerInTrigger.playerID == 1)
        {
            isInteractPressed = InputController.Instance.Player1Actions.interactionAction.IsPressed;
        }
        else if (currentPlayerInTrigger.playerID == 2)
        {
            isInteractPressed = InputController.Instance.Player2Actions.interactionAction.IsPressed;
        }

        // --- Logika Timera ---
        if (isInteractPressed)
        {
            if (!isHolding)
            {
                // Pierwsza klatka przytrzymania
                Log("Gracz ZACZĄŁ trzymać przycisk.");
                isHolding = true;

                HoldProgressService.Show(this);
                currentPlayerInTrigger.OnActionStarted.Invoke(); // WYZWALANIE ROZPOCZĘCIA
            }

            currentHoldTime += Time.deltaTime;

            if (currentHoldTime >= maxHoldTime)
            {
                Log("SUKCES! Osiągnięto maxHoldTime.");

                OnHoldComplete.Invoke();
                HoldProgressService.Hide();

                currentPlayerInTrigger.OnActionFinished.Invoke(); // WYZWALANIE ZAKOŃCZENIA
                holdWasCompleted = true;
            }
        }
        else
        {
            // Gracz NIE trzyma przycisku
            if (isHolding)
            {
                // Gracz właśnie puścił przycisk
                Log("Gracz PUŚCIŁ przycisk przed czasem. ANULOWANIE.");

                HoldProgressService.Hide();
                currentPlayerInTrigger.OnActionFinished.Invoke(); // WYZWALANIE ZAKOŃCZENIA (anulowanie)

                isHolding = false;
                currentHoldTime = 0f;
            }
        }
    }
}