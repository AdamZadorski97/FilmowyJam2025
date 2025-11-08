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
                Debug.LogError("ScoreManager nie został znaleziony w scenie! Nie można dodać punktów.");
            }
            return ScoreManager.Instance;
        }
    }

    public static void AddPoints(int playerID, int points)
    {
        if (ManagerInstance != null)
        {
            ManagerInstance.AddPoints(playerID, points);
        }
    }
}