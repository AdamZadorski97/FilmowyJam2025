using UnityEngine;
using UnityEngine.UI;
using TMPro; // Wymagane dla TextMeshPro

/// <summary>
/// Statyczny Manager UI (Singleton) do zarządzania postępem przytrzymania dla P1 i P2.
/// Musi być umieszczony JEDEN RAZ na Canvasie w scenie.
/// </summary>
public class HoldProgressManager : MonoBehaviour
{
    public static HoldProgressManager Instance { get; private set; }

    [Header("Komponenty UI - Paski Postępu")]
    [SerializeField] private Image progressBarP1Image;
    [SerializeField] private Image progressBarP2Image;

    // NOWOŚĆ: Pola dla tekstu postępu TMPro
    [Header("Komponenty UI - Tekst Postępu")]
    [SerializeField] private TMP_Text progressTextP1;
    [SerializeField] private TMP_Text progressTextP2;

    [Header("Ustawienia Gry")]
    [Tooltip("Liczba punktów przyznawana za pomyślne ukończenie przytrzymania")]
    [SerializeField] private int pointsOnCompletion = 10;

    [Header("Status Śledzenia")]
    private HoldInteractor _interactorToTrackP1;
    private HoldInteractor _interactorToTrackP2;

    private float _lastProgressP1 = 0f;
    private float _lastProgressP2 = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;

            // Inicjalizacja widoczności
            if (progressBarP1Image != null) progressBarP1Image.gameObject.SetActive(false);
            if (progressBarP2Image != null) progressBarP2Image.gameObject.SetActive(false);
            if (progressTextP1 != null) progressTextP1.gameObject.SetActive(false);
            if (progressTextP2 != null) progressTextP2.gameObject.SetActive(false);
        }
    }

    public void Show(HoldInteractor interactorToTrack, int playerID)
    {
        if (playerID == 1)
        {
            if (progressBarP1Image == null) return;
            progressBarP1Image.gameObject.SetActive(true);
            if (progressTextP1 != null) progressTextP1.gameObject.SetActive(true);
            _interactorToTrackP1 = interactorToTrack;
            _lastProgressP1 = 0f;
            UpdateProgressText(progressTextP1, 0f); // Ustaw na 0% na start
        }
        else if (playerID == 2)
        {
            if (progressBarP2Image == null) return;
            progressBarP2Image.gameObject.SetActive(true);
            if (progressTextP2 != null) progressTextP2.gameObject.SetActive(true);
            _interactorToTrackP2 = interactorToTrack;
            _lastProgressP2 = 0f;
            UpdateProgressText(progressTextP2, 0f); // Ustaw na 0% na start
        }
    }

    /// <summary>
    /// Ukrywa odpowiedni pasek postępu i przyznaje punkty (jeśli akcja się powiodła).
    /// </summary>
    public void Hide(int playerID)
    {
        bool wasSuccessful = false;

        if (playerID == 1)
        {
            if (_interactorToTrackP1 != null && _interactorToTrackP1.GetHoldProgress() >= 1.0f)
            {
                wasSuccessful = true;
            }
            if (progressBarP1Image != null) progressBarP1Image.gameObject.SetActive(false);
            if (progressTextP1 != null) progressTextP1.gameObject.SetActive(false);
            _interactorToTrackP1 = null;
        }
        else if (playerID == 2)
        {
            if (_interactorToTrackP2 != null && _interactorToTrackP2.GetHoldProgress() >= 1.0f)
            {
                wasSuccessful = true;
            }
            if (progressBarP2Image != null) progressBarP2Image.gameObject.SetActive(false);
            if (progressTextP2 != null) progressTextP2.gameObject.SetActive(false);
            _interactorToTrackP2 = null;
        }

       
    }

    private void Update()
    {
        // Aktualizacja paska dla Gracza 1
        UpdateProgress(
            ref _interactorToTrackP1,
            progressBarP1Image,
            progressTextP1,
            ref _lastProgressP1
        );

        // Aktualizacja paska dla Gracza 2
        UpdateProgress(
            ref _interactorToTrackP2,
            progressBarP2Image,
            progressTextP2,
            ref _lastProgressP2
        );
    }

    /// <summary>
    /// Prywatna metoda ułatwiająca aktualizację i sprawdzająca, czy postęp zmienił się o co najmniej 1%.
    /// </summary>
    private void UpdateProgress(
        ref HoldInteractor interactor,
        Image progressBar,
        TMP_Text progressText,
        ref float lastProgress)
    {
        if (interactor == null || progressBar == null) return;

        float currentProgress = interactor.GetHoldProgress();

        // Zmniejszenie częstotliwości aktualizacji tekstu: aktualizujemy tylko, jeśli postęp zmienił się o co najmniej 1%
        if (Mathf.Abs(currentProgress - lastProgress) >= 0.01f || currentProgress >= 1.0f)
        {
            // 1. Aktualizacja paska
            progressBar.fillAmount = currentProgress;

            // 2. Aktualizacja tekstu w formacie X%
            UpdateProgressText(progressText, currentProgress);

            // 3. Zapisanie aktualnego postępu
            lastProgress = currentProgress;
        }
    }

    private void UpdateProgressText(TMP_Text text, float progress)
    {
        if (text != null)
        {
            int percentage = Mathf.RoundToInt(progress * 100f);
            text.text = $"{percentage}%";
        }
    }
}