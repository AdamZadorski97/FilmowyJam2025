using UnityEngine;
using UnityEngine.AI; // Wymagane dla NavMeshObstacle
using DG.Tweening;
using Sirenix.OdinInspector;

public class DoorController : MonoBehaviour
{
    // Konfigurowalne zmienne w Inspektorze:
    [Header("Parametry Rotacji")]
    [Tooltip("Kąt w stopniach, o który drzwi mają się obrócić, aby się OTWORZYĆ (np. 90)")]
    public float openAngle = 90f;

    [Tooltip("Oś, wokół której drzwi się obracają (np. Vector3.up dla obrotu w płaszczyźnie poziomej)")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("Parametry Animacji")]
    [Tooltip("Czas trwania animacji otwierania/zamykania (w sekundach)")]
    public float animationDuration = 1.0f;

    public Ease easeType = Ease.OutQuad;

    // Prywatne zmienne
    private bool isOpen = false;
    private Quaternion closedRotation;
    private NavMeshObstacle navMeshObstacle; // Referencja do komponentu

    void Awake()
    {
        // Pobranie komponentu NavMeshObstacle, jeśli istnieje
        navMeshObstacle = GetComponent<NavMeshObstacle>();
    }

    void Start()
    {
        // Zapisanie początkowej lokalnej rotacji jako pozycji 'zamkniętej'.
        closedRotation = transform.localRotation;

        // Drzwi na starcie są zamknięte, więc przeszkoda jest aktywna
        UpdateNavMeshObstacle(true);
    }

    // ---------------------- FUNKCJE PUBLICZNE ----------------------

    /// <summary>
    /// Przełącza stan drzwi: otwiera je, jeśli są zamknięte, lub zamyka, jeśli są otwarte.
    /// </summary>
    [Button]
    public void ToggleDoor()
    {
        if (isOpen)
        {
            CloseDoor();
        }
        else
        {
            OpenDoor();
        }
    }

    /// <summary>
    /// Otwiera drzwi (rotacja) i wyłącza przeszkodę NavMesh.
    /// </summary>
    public void OpenDoor()
    {
        if (isOpen) return;

        // 1. Wyłączenie NavMeshObstacle, aby agenci mogli przejść
        UpdateNavMeshObstacle(false);

        // 2. Obliczenie rotacji docelowej
        Quaternion targetRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);

        // 3. Animacja
        transform.DOLocalRotateQuaternion(targetRotation, animationDuration)
                 .SetEase(easeType)
                 .OnComplete(() => isOpen = true);

        Debug.Log("Drzwi: Otwieranie...");
    }

    /// <summary>
    /// Zamyka drzwi (rotacja) i włącza przeszkodę NavMesh.
    /// </summary>
    public void CloseDoor()
    {
        if (!isOpen) return;

        // 1. Włączenie NavMeshObstacle, aby agenci uznali drzwi za barierę
        // UWAGA: Aktywujemy przeszkodę na starcie animacji, a nie na końcu,
        // aby agenci nie próbowali przejść, gdy drzwi są już w ruchu.
        UpdateNavMeshObstacle(true);

        // 2. Animacja
        transform.DOLocalRotateQuaternion(closedRotation, animationDuration)
                 .SetEase(easeType)
                 .OnComplete(() => isOpen = false);

        Debug.Log("Drzwi: Zamykanie...");
    }

    // ---------------------- FUNKCJA NAVMESH ----------------------

    /// <summary>
    /// Aktywuje lub dezaktywuje komponent NavMeshObstacle.
    /// </summary>
    /// <param name="active">True, aby zablokować drogę; False, aby odblokować.</param>
    private void UpdateNavMeshObstacle(bool active)
    {
        if (navMeshObstacle != null)
        {
            navMeshObstacle.enabled = active;

            if (active)
            {
                // Włączenie 'Carving' powoduje, że NavMesh zostanie wycięty, 
                // co zmusza agentów do znajdowania nowej ścieżki wokół drzwi.
                navMeshObstacle.carving = true;
            }
            else
            {
                // Wyłączenie 'Carving' oznacza, że przeszkoda przestaje blokować
                navMeshObstacle.carving = false;
            }
        }
    }
}