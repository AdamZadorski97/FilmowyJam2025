using UnityEngine;
using InControl;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.UI;
public class InputHintController : MonoBehaviour
{
    private static BindingProperties _bindingProperties;
    private static GameObject _hintPrefab;
    private static Transform _hintContainer;

    private static GameObject _currentHintInstance;
    private static Coroutine _hideCoroutine;
    private static Tweener _fadeTween;

    /// <summary>
    /// Initializes the Manager. Must be called once at the start of the game.
    /// </summary>
    public static void Init(BindingProperties properties, GameObject prefab)
    {
        _bindingProperties = properties;
        _hintPrefab = prefab;
    }

    /// <summary>
    /// Displays a hint on the screen with a fade-in effect.
    /// </summary>
    /// <param name="actionName">The name of the action from BindingProperties (e.g., "Interact")</param>
    /// <param name="message">The hint text to display.</param>
    public static void Show(string actionName, string message)
    {
        if (_bindingProperties == null || _hintPrefab == null)
        {
            Debug.LogError("HintManager is not initialized! Ensure an object with HintManagerInitializer is in the scene.");
            return;
        }

        // If a hint is already showing or fading out, hide it instantly before showing the new one.
        if (_currentHintInstance != null)
        {
            // Kill any active tweens on the existing hint to prevent conflicts.
            if (_fadeTween.IsActive())
            {
                _fadeTween.Kill();
            }
            Destroy(_currentHintInstance);
        }

        // Create the hint instance
        _currentHintInstance = Instantiate(_hintPrefab, _hintContainer);
        HintUI hintUI = _currentHintInstance.GetComponent<HintUI>();
        CanvasGroup canvasGroup = hintUI.canvasGroup;
        HorizontalLayoutGroup layoutGroup = hintUI.layoutGroup;

        if (hintUI == null || canvasGroup == null || layoutGroup == null)
        {
            Debug.LogError("The hint prefab must have HintUI, CanvasGroup, and a child HorizontalLayoutGroup components.", _hintPrefab);
            Destroy(_currentHintInstance); // Clean up
            return;
        }

        // Set the content
        hintUI.hintText.text = message;
        Sprite buttonSprite = _bindingProperties.GetBindingSprite(actionName);

        if (buttonSprite != null)
        {
            hintUI.buttonImage.sprite = buttonSprite;
            hintUI.buttonImage.enabled = true;
        }
        else
        {
            hintUI.buttonImage.enabled = false;
        }

        // --- Refresh Layout Group ---
        // Disable and then re-enable the layout group to force it to recalculate its layout.
        // This is a reliable way to ensure the elements are correctly positioned after content changes.
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
            layoutGroup.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
        }
      

        // --- Fade In Effect ---
        canvasGroup.alpha = 0f; // Start transparent
        _fadeTween = canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad); // Fade in over 0.3 seconds
    }

    /// <summary>
    /// Hides the currently displayed hint with a fade-out effect.
    /// </summary>
    public static void Hide()
    {
        if (_currentHintInstance != null)
        {
            // Kill any existing fade-in tween
            if (_fadeTween.IsActive())
            {
                _fadeTween.Kill();
            }

            CanvasGroup canvasGroup = _currentHintInstance.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                // Fade out, and on completion, destroy the object
                _fadeTween = canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    if (_currentHintInstance != null)
                    {
                        Destroy(_currentHintInstance);
                        _currentHintInstance = null;
                    }
                });
            }
            else
            {
                // If no canvas group, just destroy it immediately
                Destroy(_currentHintInstance);
                _currentHintInstance = null;
            }
        }
    }
}