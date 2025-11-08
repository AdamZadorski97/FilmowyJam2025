using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
            this.trackedInteractor = null;
        });
    }

    private void Update()
    {
        // Jeśli nie śledzimy interaktora (bo jesteśmy ukryci), nic nie rób
        if (trackedInteractor == null) return;

        // Aktualizuj fillAmount na podstawie postępu śledzonego interaktora
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = trackedInteractor.GetHoldProgress();
        }
    }
}