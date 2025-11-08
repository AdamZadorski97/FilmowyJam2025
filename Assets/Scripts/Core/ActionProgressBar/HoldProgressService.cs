using UnityEngine;

/// <summary>
/// Statyczny serwis do zarządzania paskami postępu. 
/// Działa jako "most" do instancji Singletona HoldProgressManager w scenie.
/// **NAPRAWIA BŁĘDY KOMPILACJI CS7036 i CS1061.**
/// </summary>
public static class HoldProgressService
{
    // Nie potrzebujemy już prefabu, ale zostawiamy Init, jeśli jest wywoływane gdzie indziej.
 

    /// <summary>
    /// Pokazuje i aktywuje pasek postępu dla danego gracza.
    /// </summary>
    public static void Show(HoldInteractor interactorToTrack, int playerID)
    {
        if (HoldProgressManager.Instance == null)
        {
            Debug.LogError("HoldProgressManager (Singleton) nie został znaleziony w scenie! Upewnij się, że obiekt z tym skryptem istnieje.");
            return;
        }

        // Przekazanie wywołania do Singletona
        HoldProgressManager.Instance.Show(interactorToTrack, playerID);
    }

    /// <summary>
    /// Ukrywa pasek postępu dla danego gracza.
    /// </summary>
    public static void Hide(int playerID)
    {
        if (HoldProgressManager.Instance == null)
        {
            // Możemy po prostu zignorować, jeśli Singletona nie ma, bo to tylko Hide.
            return;
        }

        // Przekazanie wywołania do Singletona
        HoldProgressManager.Instance.Hide(playerID);
    }
}