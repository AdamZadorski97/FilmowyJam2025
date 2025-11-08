using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Używamy DOTween do płynnego pokazywania/ukrywania

/// <summary>
/// Ten skrypt wizualizuje postęp z HoldInteractor na obrazku UI (Image).
/// Wymaga komponentu CanvasGroup do płynnego pokazywania/ukrywania.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class HoldProgressUI : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField]
    [Tooltip("Obrazek, który będzie pełnił rolę paska postępu (musi być typu 'Filled')")]
    private Image progressBarImage;

    [SerializeField]
    [Tooltip("Referencja do interaktora, którego postęp wizualizujemy")]
    private HoldInteractor holdInteractor;

    [Header("Ustawienia")]
    [SerializeField]
    [Tooltip("Jak szybko pasek ma się pojawić/zniknąć")]
    private float fadeDuration = 0.2f;

    private CanvasGroup canvasGroup;
    private Tweener fadeTween;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // Upewnij się, że na starcie wszystko jest ukryte i wyzerowane
        canvasGroup.alpha = 0f;
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = 0f;
        }
        else
        {
            Debug.LogError("HoldProgressUI: Nie przypisano 'ProgressBarImage'!", this);
        }

        if (holdInteractor == null)
        {
            Debug.LogError("HoldProgressUI: Nie przypisano 'HoldInteractor'!", this);
        }
    }

    private void Update()
    {
        // Jeśli interaktor nie jest przypisany, nic nie rób
        if (holdInteractor == null) return;

        // Ciągle aktualizuj wizualny pasek postępu o dane z logiki interaktora.
        // Sama widoczność (alpha) jest kontrolowana przez metody Show/Hide.
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = holdInteractor.GetHoldProgress();
        }
    }

    // --- Publiczne Metody (do podpięcia w UnityEvents) ---

    /// <summary>
    /// Płynnie pokazuje pasek postępu.
    /// (Do podpięcia pod HoldInteractor.OnHoldStarted)
    /// </summary>
    public void ShowProgressBar()
    {
        if (fadeTween.IsActive()) fadeTween.Kill();
        fadeTween = canvasGroup.DOFade(1f, fadeDuration);
    }

    /// <summary>
    /// Płynnie ukrywa pasek postępu.
    /// (Do podpięcia pod HoldInteractor.OnHoldComplete)
    /// </summary>
    public void HideProgressBar()
    {
        if (fadeTween.IsActive()) fadeTween.Kill();
        fadeTween = canvasGroup.DOFade(0f, fadeDuration);
    }

    /// <summary>
    /// Płynnie ukrywa i natychmiast resetuje pasek postępu.
    /// (Do podpięcia pod HoldInteractor.OnHoldCancelled)
    /// </summary>
    public void HideAndResetProgressBar()
    {
        HideProgressBar();
        // Resetuj fillAmount natychmiast, nawet jeśli fade-out jeszcze trwa
        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = 0f;
        }
    }
}