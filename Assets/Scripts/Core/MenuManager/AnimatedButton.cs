using UnityEngine;
using UnityEngine.EventSystems; // Potrzebne dla ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
using DG.Tweening; // Potrzebne dla DoTween

public class AnimatedButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler // Dodano ISubmitHandler
{
    private Vector3 originalScale;

    public Vector3 selectedScale = new Vector3(1.1f, 1.1f, 1.1f);
    public float scaleDuration = 0.2f;

    [Header("Sound Names")]
    public string selectSoundName = "ButtonSelect";
    public string clickSoundName = "ButtonClick";

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnSelect(BaseEventData eventData)
    {
        transform.DOKill(true);
        transform.DOScale(selectedScale, scaleDuration).SetEase(Ease.OutBack);

        if (SoundManager.Instance != null && !string.IsNullOrEmpty(selectSoundName))
        {
            SoundManager.Instance.PlayUISound(selectSoundName);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        transform.DOKill(true);
        transform.DOScale(originalScale, scaleDuration).SetEase(Ease.OutQuad);
    }

    // Ta metoda reaguje na kliknięcie MYSZĄ
    public void OnPointerClick(PointerEventData eventData)
    {
        // Ważne: usuń odtwarzanie dźwięku stąd, aby uniknąć podwójnego dźwięku
        // Dźwięk będzie odtwarzany w OnSubmit
    }

    // Dodana metoda: Ta metoda reaguje na AKTYWACJĘ przycisku (np. Enter/Spacja na klawiaturze, kliknięcie myszą)
    public void OnSubmit(BaseEventData eventData)
    {
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(clickSoundName))
        {
            SoundManager.Instance.PlayUISound(clickSoundName);
        }
    }

    void OnDisable()
    {
        transform.DOKill(true);
        transform.localScale = originalScale;
    }
}