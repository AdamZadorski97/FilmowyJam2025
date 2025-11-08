using UnityEngine;

/// <summary>
/// Inicjalizuje HoldProgressService, przekazując mu prefab paska postępu.
/// Umieść ten skrypt na obiekcie w scenie (np. "Manager" lub "Services").
/// </summary>
public class HoldProgressManagerInitializer : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Prefab paska postępu (ten, który ma na sobie HoldProgressManager.cs)")]
    private GameObject holdProgressPrefab;

    private void Awake()
    {
        if (holdProgressPrefab == null)
        {
            Debug.LogError("Nie przypisano 'holdProgressPrefab' w Initializerze!", this);
            return;
        }

        HoldProgressService.Init(holdProgressPrefab);
    }
}