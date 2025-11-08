using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector; // To jest kluczowe!

public class DoorController : MonoBehaviour
{
    // Konfigurowalne zmienne w Inspektorze:
    [Header("Pozycje Drzwi")]
    [Tooltip("Lokalna pozycja, gdy drzwi są ZAMKNIĘTE (domyślna)")]
    public Vector3 closedPosition;

    [Tooltip("Lokalna pozycja, gdy drzwi są OTWARTE")]
    public Vector3 openPosition;

    [Header("Parametry Animacji")]
    [Tooltip("Czas trwania animacji otwierania/zamykania (w sekundach)")]
    public float animationDuration = 1.0f;

    [Tooltip("Rodzaj łagodzenia (easing) animacji, np. OutBack, Linear")]
    public Ease easeType = Ease.OutQuad;

    private bool isOpen = false;

    void Start()
    {
        // Ustawienie początkowej pozycji zamkniętej jako domyślnej pozycji obiektu.
        closedPosition = transform.localPosition;
    }

    /// <summary>
    /// Otwiera drzwi, przesuwając je do pozycji 'openPosition'.
    /// </summary>
    public void OpenDoor()
    {
        if (isOpen) return; // Zapobiega ponownemu wywołaniu, gdy już są otwarte

        // Użycie DOTween do animowania lokalnej pozycji obiektu
        transform.DOLocalMove(openPosition, animationDuration)
                 .SetEase(easeType)
                 .OnComplete(() => isOpen = true); // Ustawienie flagi po zakończeniu animacji

        Debug.Log("Drzwi: Otwieranie...");
    }

    /// <summary>
    /// Zamyka drzwi, przesuwając je z powrotem do pozycji 'closedPosition'.
    /// </summary>
    public void CloseDoor()
    {
        if (!isOpen) return; // Zapobiega ponownemu wywołaniu, gdy już są zamknięte

        // Użycie DOTween do animowania lokalnej pozycji obiektu
        transform.DOLocalMove(closedPosition, animationDuration)
                 .SetEase(easeType)
                 .OnComplete(() => isOpen = false); // Ustawienie flagi po zakończeniu animacji

        Debug.Log("Drzwi: Zamykanie...");
    }

    [Button]
     public void ToggleDoor()
    {
        if (isOpen)
            CloseDoor();
        else
            OpenDoor();
    }
}