using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Loading Screen Settings")]
    public CanvasGroup loadingScreenCanvasGroup;
    public Image progressFill;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI loadingTipsText;
    public float fadeDuration = 0.5f;
    public float minLoadingScreenDuration = 1.5f;

    [Header("Loading Tips")]
    public string[] loadingTips;

    private AsyncOperation currentLoadingOperation;
    private float timeElapsedOnLoadingScreen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingScreenCanvasGroup != null)
        {
            loadingScreenCanvasGroup.alpha = 0;
            loadingScreenCanvasGroup.interactable = false;
            loadingScreenCanvasGroup.blocksRaycasts = false;
            loadingScreenCanvasGroup.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("SceneLoader: Loading Screen Canvas Group not assigned! Loading screen will not work.");
        }

        if (progressFill != null)
            progressFill.fillAmount = 0;
    }

    public void LoadScene(string sceneName)
    {
        Debug.Log($"SceneLoader: Rozpoczynanie ładowania sceny: {sceneName}");
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"SceneLoader: Restartowanie sceny: {currentSceneName}");
        LoadScene(currentSceneName);
    }

    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        ShowLoadingScreen();
        yield return new WaitForSeconds(fadeDuration);

        timeElapsedOnLoadingScreen = 0f;

        if (loadingTipsText != null && loadingTips != null && loadingTips.Length > 0)
        {
            loadingTipsText.text = loadingTips[Random.Range(0, loadingTips.Length)];
        }

        currentLoadingOperation = SceneManager.LoadSceneAsync(sceneName);
        currentLoadingOperation.allowSceneActivation = false;

        while (!currentLoadingOperation.isDone)
        {
            timeElapsedOnLoadingScreen += Time.deltaTime;

            float progress = Mathf.Clamp01(currentLoadingOperation.progress / 0.9f);

            if (progressFill != null)
                progressFill.fillAmount = progress;

            if (progressText != null)
                progressText.text = $"Ładowanie... {(progress * 100):F0}%";

            if (currentLoadingOperation.progress >= 0.9f && timeElapsedOnLoadingScreen >= minLoadingScreenDuration)
            {
                currentLoadingOperation.allowSceneActivation = true;
            }

            yield return null;
        }

        HideLoadingScreen();
        Debug.Log($"SceneLoader: Scena '{sceneName}' została załadowana i aktywowana.");
    }

    private void ShowLoadingScreen()
    {
        if (loadingScreenCanvasGroup == null) return;

        loadingScreenCanvasGroup.gameObject.SetActive(true);
        loadingScreenCanvasGroup.alpha = 0;
        loadingScreenCanvasGroup.interactable = true;
        loadingScreenCanvasGroup.blocksRaycasts = true;
        loadingScreenCanvasGroup.DOKill(true);
        loadingScreenCanvasGroup.DOFade(1, fadeDuration);

        if (progressFill != null)
            progressFill.fillAmount = 0;
    }

    private void HideLoadingScreen()
    {
        if (loadingScreenCanvasGroup == null) return;

        loadingScreenCanvasGroup.DOKill(true);
        loadingScreenCanvasGroup.DOFade(0, fadeDuration).OnComplete(() =>
        {
            loadingScreenCanvasGroup.interactable = false;
            loadingScreenCanvasGroup.blocksRaycasts = false;
            loadingScreenCanvasGroup.gameObject.SetActive(false);
        });
    }
}
