using UnityEngine;

/// <summary>
/// Statyczny serwis do zarządzania globalnym paskiem postępu.
/// Działa jako "most" do instancji prefabu UI.
/// </summary>
public static class HoldProgressService
{
    private static GameObject _prefab;
    private static HoldProgressManager _currentInstance; // Referencja do skryptu na prefabie

    /// <summary>
    /// Inicjalizuje serwis. Musi być wywołane raz na starcie gry.
    /// </summary>
    public static void Init(GameObject prefab)
    {
        _prefab = prefab;
    }

    /// <summary>
    /// Pokazuje i aktywuje pasek postępu, aby śledził dany interaktor.
    /// </summary>
    public static void Show(HoldInteractor interactorToTrack)
    {
        if (_prefab == null)
        {
            Debug.LogError("HoldProgressService NIE ZOSTAŁ ZAINICJALIZOWANY! " +
                           "Upewnij się, że masz w scenie HoldProgressManagerInitializer.");
            return;
        }

        // Jeśli instancja nie istnieje (np. pierwszy raz, lub po zmianie sceny), stwórz ją.
        if (_currentInstance == null)
        {
            GameObject instanceObj = Object.Instantiate(_prefab);
            _currentInstance = instanceObj.GetComponent<HoldProgressManager>();

            if (_currentInstance == null)
            {
                Debug.LogError($"Prefab '{_prefab.name}' nie ma na sobie skryptu HoldProgressManager!", _prefab);
                Object.Destroy(instanceObj); // posprzątaj
                return;
            }
        }

        // Przekaż polecenie do instancji
        _currentInstance.Show(interactorToTrack);
    }

    /// <summary>
    /// Ukrywa pasek postępu.
    /// </summary>
    public static void Hide()
    {
        if (_currentInstance != null)
        {
            _currentInstance.Hide();
        }
    }
}