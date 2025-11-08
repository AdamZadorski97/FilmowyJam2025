using UnityEngine;
using UnityEngine.Events;

// Upewnij się, że masz te usingi
// (Jeśli używasz wersji z logami, zachowaj ją)

public class HoldInteractor : MonoBehaviour
{
    [Header("Ustawienia Interakcji")]
    [SerializeField]
    private float maxHoldTime = 2.0f;

    [Header("Eventy")]
    [Tooltip("Wywoływane, gdy gracz PRAWIDŁOWO przytrzyma przycisk przez 'maxHoldTime'")]
    public UnityEvent OnHoldComplete;

    // ----- USUNIĘTE UNITY EVENTS -----
    // public UnityEvent OnHoldStarted;     <-- Usuń to
    // public UnityEvent OnHoldCancelled;   <-- Usuń to

    // --- Zmienne prywatne ---
    private PlayerController currentPlayerInTrigger;
    private float currentHoldTime = 0f;
    private bool isHolding = false;
    private bool holdWasCompleted = false;

    // --- DEBUG ---
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    private void Log(string message) { /*...*/ } // (Zachowaj swoją funkcję Log)
    // --- KONIEC DEBUG ---

    public float GetHoldProgress()
    {
        if (maxHoldTime <= 0) return 0;
        return Mathf.Clamp01(currentHoldTime / maxHoldTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // ... (bez zmian) ...
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

                // OnHoldCancelled.Invoke(); // <-- ZAMIEŃ TO...
                HoldProgressService.Hide(); // <-- ...NA TO.
            }

            Log("...Czyszczę referencję do gracza.");
            currentPlayerInTrigger = null;
            currentHoldTime = 0f;
            isHolding = false;
            holdWasCompleted = false;
        }
        // ... (reszta bez zmian) ...
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

                // OnHoldStarted.Invoke(); // <-- ZAMIEŃ TO...
                HoldProgressService.Show(this); // <-- ...NA TO. (Przekazujemy 'this' do śledzenia)
            }

            currentHoldTime += Time.deltaTime;
            // Log($"Postęp: {currentHoldTime} / {maxHoldTime}"); // (Odkomentuj w razie potrzeby)

            if (currentHoldTime >= maxHoldTime)
            {
                Log("SUKCES! Osiągnięto maxHoldTime.");

                OnHoldComplete.Invoke();    // <-- ZACHOWAJ TO (dla logiki gry)
                HoldProgressService.Hide(); // <-- DODAJ TO (dla UI)

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

                // OnHoldCancelled.Invoke(); // <-- ZAMIEŃ TO...
                HoldProgressService.Hide(); // <-- ...NA TO.

                isHolding = false;
                currentHoldTime = 0f;
            }
        }
    }
}