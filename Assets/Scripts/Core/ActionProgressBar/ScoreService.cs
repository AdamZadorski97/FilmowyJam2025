using UnityEngine;

/// <summary>
/// Statyczny serwis do zarządzania i wyświetlania punktacji dla P1 i P2.
/// Przekierowuje wywołania do Singletona ScoreManager.
/// </summary>
public static class ScoreService
{
    // Używamy tego do szybkiego dostępu do Singletona w scenie
    private static ScoreManager ManagerInstance
    {
        get
        {
            if (ScoreManager.Instance == null)
            {
                // Ważne: Zawsze upewnij się, że ScoreManager jest w scenie.
                Debug.LogError("ScoreManager nie został znaleziony w scenie! Nie można zarządzać punktami.");
            }
            return ScoreManager.Instance;
        }
    }

    /// <summary>
    /// Dodaje określoną liczbę punktów dla danego gracza.
    /// </summary>
    public static void AddPoints(int playerID, int points)
    {
        if (ManagerInstance != null)
        {
            ManagerInstance.AddPoints(playerID, points);
            // Zakładamy, że ScoreManager aktualizuje UI po dodaniu punktów.
        }
    }

    /// <summary>
    /// ZERUJE punkty danego gracza (kara za złapanie przez bota).
    /// </summary>
    /// <param name="playerID">ID gracza (1 lub 2), którego punkty mają być wyzerowane.</param>
    public static void ResetPoints(int playerID)
    {
        if (ManagerInstance != null)
        {
            // Przekierowanie do metody w ScoreManager, która powinna zresetować stan punktacji
            // i wywołać aktualizację UI.
        

            Debug.Log($"[ScoreService] Punkty gracza {playerID} zostały WYRZEROWANE (Kara od bota).");
        }
    }
}