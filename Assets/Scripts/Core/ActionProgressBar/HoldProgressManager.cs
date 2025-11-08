using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro; // **DODAJ TO!**

/// <summary>
/// Główny skrypt 'driver' dla prefabu paska postępu.
/// Mieszka na prefabie i wykonuje operacje (fade, update fill).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class HoldProgressManager : MonoBehaviour
{
    [Header("Referencje (z Prefaba)")]
    [SerializeField]
    [Tooltip("Obrazek, który będzie pełnił rolę paska postępu (musi być typu 'Filled')")]
    private Image progressBarImage;

    [SerializeField]
    [Tooltip("Opcjonalne: Tekst (TextMeshProUGUI) do wyświetlania % postępu")]
    private TextMeshProUGUI progressText; // **NOWE POLE**

    private CanvasGroup canvasGroup;
    private Tweener fadeTween;

    // Interaktor, który obecnie śledzimy
    private HoldInteractor trackedInteractor;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // Upewnij się, że na starcie jest ukryty
        canvasGroup.alpha = 0f;
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = 0f;
        }
        else
        {
            Debug.LogError("HoldProgressManager: Prefab nie ma przypisanego 'progressBarImage'!");
        }

        // **NOWA ZMIANA:** Ustawienie domyślnego tekstu na starcie
        if (progressText != null)
        {
            progressText.text = "0%";
        }

        // Ustaw, aby ten obiekt UI przetrwał zmiany sceny, tak jak manager
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Wewnętrzna funkcja pokazywania (wywoływana przez serwis).
    /// </summary>
    public void Show(HoldInteractor interactorToTrack)
    {
        this.trackedInteractor = interactorToTrack;

        if (trackedInteractor == null)
        {
            Debug.LogError("HoldProgressManager.Show został wywołany z 'null' interactor!");
            Hide(); // Ukryj, jeśli coś jest nie tak
            return;
        }

        // Zresetuj pasek i pokaż go
        progressBarImage.fillAmount = 0f;

        // **NOWA ZMIANA:** Zresetuj tekst
        if (progressText != null)
        {
            progressText.text = "0%";
        }

        if (fadeTween.IsActive()) fadeTween.Kill();
        fadeTween = canvasGroup.DOFade(1f, 0.2f);
    }

    /// <summary>
    /// Wewnętrzna funkcja ukrywania (wywoływana przez serwis).
    /// </summary>
    public void Hide()
    {
        if (fadeTween.IsActive()) fadeTween.Kill();
        fadeTween = canvasGroup.DOFade(0f, 0.2f).OnComplete(() => {
            // Po ukryciu, zresetuj i przestań śledzić
            if (progressBarImage != null)
            {
                progressBarImage.fillAmount = 0f;
            }
            // **NOWA ZMIANA:** Zresetuj tekst po zniknięciu
            if (progressText != null)
            {
                progressText.text = "0%";
            }
            this.trackedInteractor = null;
        });
    }

    private void Update()
    {
        // Jeśli nie śledzimy interaktora (bo jesteśmy ukryci), nic nie rób
        if (trackedInteractor == null) return;

        // Pobierz aktualny postęp (w zakresie 0.0 do 1.0)
        float progress = trackedInteractor.GetHoldProgress();

        // Aktualizuj fillAmount na podstawie postępu śledzonego interaktora
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = progress;
        }

        // **NOWA ZMIANA:** Aktualizuj tekst z procentowym postępem
        if (progressText != null)
        {
            // Przelicz postęp na procent (0-100) i zaokrąglij do całości, dodając symbol %
            int percent = Mathf.FloorToInt(progress * 100f);
            progressText.text = $"{percent}%";
        }
    }
}