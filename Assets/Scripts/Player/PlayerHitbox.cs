using UnityEngine;

/// <summary>
/// Hitbox do wykrywania kolizji uderzeń (Punch/Kick).
/// Powinien być dołączony do obiektu z Colliderem ustawionym jako Is Trigger.
/// </summary>
public class PlayerHitbox : MonoBehaviour
{
    [Header("Ustawienia Hitboxa")]
    [SerializeField] private int scoreDamage = 5; // Ile punktów odejmie cios

    [Tooltip("Gracz, do którego należy ten Hitbox (agresor).")]
    public PlayerController ownerController;

    private bool hasHit = false; // Zapobieganie wielokrotnym trafieniom w jednym uderzeniu

    private void Awake()
    {
        // Sprawdzenie, czy ownerController jest przypisany, ewentualnie poszukanie go w rodzicu
        if (ownerController == null)
        {
            ownerController = GetComponentInParent<PlayerController>();
        }
        if (ownerController == null)
        {
            Debug.LogError("PlayerController nie został znaleziony w rodzicu Hitboxa. Upewnij się, że PlayerController jest komponentem rodzica.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Jeśli już uderzyliśmy w tym cyklu lub nie mamy właściciela, ignoruj
        if (hasHit || ownerController == null) return;

        // Próba pobrania PlayerController z trafionego obiektu
        PlayerController targetPlayer = other.GetComponent<PlayerController>();

        // Upewnij się, że:
        // 1. Trafiliśmy w innego gracza.
        // 2. To nie jest nasz własny hitbox.
        if (targetPlayer != null && targetPlayer.playerID != ownerController.playerID)
        {
            // Trafienie!
            hasHit = true; // Zablokuj dalsze trafienia w tym cyklu uderzenia

            // ODEJMOWANIE PUNKTÓW ZAMIAST ZDROWIA
            if (ScoreManager.Instance != null)
            {
                // Odejmowanie punktów od trafionego gracza
                ScoreManager.Instance.AddPoints(targetPlayer.playerID, -scoreDamage);
            }

            // Odtwarzanie efektów (np. stun, wibracje, dźwięki)
            targetPlayer.GetHit();
            targetPlayer.SetStunned(0.25f, 0.5f); // Lekki stun po trafieniu

            Debug.Log($"Gracz {ownerController.playerID} uderzył Gracza {targetPlayer.playerID}. Odebrano {scoreDamage} punktów.");

            // UWAGA: Wyłączenie/włączenie Hitboxa musi być kontrolowane przez PlayerController
            // po zakończeniu animacji Punch/Kick.
        }
    }

    /// <summary>
    /// Resetuje stan Hitboxa po zakończeniu ataku.
    /// Metoda ta powinna być wywołana przez PlayerController po wyłączeniu Hitboxa.
    /// </summary>
    public void ResetHitbox()
    {
        hasHit = false;
    }

    // Opcjonalne: Użyj tej metody, jeśli chcesz dostosować obrażenia np. dla Punch/Kick
    public void SetScoreDamage(int newDamage)
    {
        scoreDamage = newDamage;
    }
}