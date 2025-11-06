using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using System.Collections.Generic;
// Usunięto System.Collections, ponieważ korutyny są teraz w SceneLoader

public class MainMenuCore : MonoBehaviour
{
    public CanvasGroup mainMenuPanelCG;
    public CanvasGroup creditsPanelCG;
    // Usunięto loadingScreenPanelCG, ponieważ jest on zarządzany przez SceneLoader

    public Button firstMainMenuButton;
    public Button firstCreditsButton;

    public string gameSceneName = "Game"; // Nadal potrzebujemy nazwy sceny do przekazania

    public Vector3 selectedScale = new Vector3(1.1f, 1.1f, 1.1f);
    public float scaleDuration = 0.2f;
    public string selectSoundName = "ButtonSelect";
    public string clickSoundName = "ButtonClick";

    public float fadeDuration = 0.3f; // Czas fade dla paneli menu

    private Stack<CanvasGroup> panelHistory = new Stack<CanvasGroup>();

    void Awake()
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("Brak EventSystem w scenie! Menu UI nie będzie działać poprawnie. Dodaj EventSystem (GameObject > UI > Event System).");
        }

        SetupCanvasGroup(mainMenuPanelCG);
        SetupCanvasGroup(creditsPanelCG);
        // LoadingScreenPanelCG nie jest już konfigurowany tutaj

        SetupButtons(mainMenuPanelCG.gameObject, "MainMenu");
        SetupButtons(creditsPanelCG.gameObject, "Credits");
    }

    void Start()
    {
        ShowMainMenu();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }
    }

    private void SetupCanvasGroup(CanvasGroup cg)
    {
        if (cg == null)
        {
            Debug.LogError("CanvasGroup is null. Please assign it in the Inspector.");
            return;
        }
        cg.alpha = 0;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.gameObject.SetActive(false);
    }

    private void SetupButtons(GameObject panel, string panelType)
    {
        if (panel == null) return;

        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            AnimatedButton animatedBtn = btn.GetComponent<AnimatedButton>();
            if (animatedBtn == null)
            {
                animatedBtn = btn.gameObject.AddComponent<AnimatedButton>();
            }

            animatedBtn.selectedScale = selectedScale;
            animatedBtn.scaleDuration = scaleDuration;
            animatedBtn.selectSoundName = selectSoundName;
            animatedBtn.clickSoundName = clickSoundName;

            btn.onClick.RemoveAllListeners();

            if (btn.name.Contains("Start"))
            {
                btn.onClick.AddListener(StartGame); // Nadal wywołuje StartGame
            }
            else if (btn.name.Contains("Credits"))
            {
                btn.onClick.AddListener(ShowCredits);
            }
            else if (btn.name.Contains("Exit") || btn.name.Contains("Quit"))
            {
                btn.onClick.AddListener(ExitGame);
            }
            else if (btn.name.Contains("Back") || btn.name.Contains("Return"))
            {
                btn.onClick.AddListener(GoBack);
            }
            else
            {
                Debug.LogWarning($"Przycisk '{btn.name}' w panelu '{panel.name}' nie ma przypisanej akcji w MainMenuCore.");
            }
        }
    }

    private void SetActivePanel(CanvasGroup panelToShowCG)
    {
        if (panelToShowCG == null) return;

        if (panelHistory.Count > 0)
        {
            CanvasGroup currentPanelCG = panelHistory.Peek();
            if (currentPanelCG != panelToShowCG)
            {
                currentPanelCG.DOFade(0, fadeDuration).OnComplete(() => {
                    currentPanelCG.interactable = false;
                    currentPanelCG.blocksRaycasts = false;
                    currentPanelCG.gameObject.SetActive(false);
                });
            }
        }

        panelToShowCG.gameObject.SetActive(true);
        panelToShowCG.alpha = 0;
        panelToShowCG.interactable = true;
        panelToShowCG.blocksRaycasts = true;
        panelToShowCG.DOFade(1, fadeDuration).OnComplete(() => {
            Button firstButton = GetFirstButtonInPanel(panelToShowCG.gameObject);
            if (firstButton != null)
            {
                StartCoroutine(SelectButtonDelayed(firstButton));
            }
        });

        if (panelHistory.Count == 0 || panelHistory.Peek() != panelToShowCG)
        {
            panelHistory.Push(panelToShowCG);
        }
    }

    private Button GetFirstButtonInPanel(GameObject panel)
    {
        if (panel == null) return null;
        Button[] buttons = panel.GetComponentsInChildren<Button>(false);
        if (buttons.Length > 0)
        {
            return buttons[0];
        }
        return null;
    }

    public void ShowMainMenu()
    {
        panelHistory.Clear();
        mainMenuPanelCG.gameObject.SetActive(false);
        creditsPanelCG.gameObject.SetActive(false);

        SetActivePanel(mainMenuPanelCG);
        Debug.Log("Wyświetlono główne menu.");
    }

    public void ShowCredits()
    {
        SetActivePanel(creditsPanelCG);
        Debug.Log("Wyświetlono panel autorów.");
    }

    /// <summary>
    /// Rozpoczyna ładowanie sceny gry za pośrednictwem SceneLoadera.
    /// </summary>
    public void StartGame()
    {
        Debug.Log("MainMenuCore: Przygotowanie do ładowania gry...");
        // Ukryj wszystkie panele menu z animacją
        mainMenuPanelCG.DOFade(0, fadeDuration).OnComplete(() => mainMenuPanelCG.gameObject.SetActive(false));
        creditsPanelCG.DOFade(0, fadeDuration).OnComplete(() => creditsPanelCG.gameObject.SetActive(false));

        // Po zakończeniu fade-out, wywołaj SceneLoader
        DOVirtual.DelayedCall(fadeDuration, () => {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(gameSceneName);
            }
            else
            {
                Debug.LogError("SceneLoader nie jest zainicjowany! Nie można załadować sceny.");
                SceneManager.LoadScene(gameSceneName); // Awaryjne ładowanie bez ekranu
            }
        });
    }

    public void ExitGame()
    {
        Debug.Log("Wyjście z gry...");
        mainMenuPanelCG.DOFade(0, fadeDuration).OnComplete(() => {
            mainMenuPanelCG.gameObject.SetActive(false);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        });
        creditsPanelCG.DOFade(0, fadeDuration).OnComplete(() => creditsPanelCG.gameObject.SetActive(false));
    }

    public void GoBack()
    {
        if (panelHistory.Count > 1)
        {
            CanvasGroup currentPanel = panelHistory.Pop();
            currentPanel.DOFade(0, fadeDuration).OnComplete(() => {
                currentPanel.interactable = false;
                currentPanel.blocksRaycasts = false;
                currentPanel.gameObject.SetActive(false);
            });

            CanvasGroup previousPanel = panelHistory.Peek();
            SetActivePanel(previousPanel);
            Debug.Log($"Wrócono do panelu: {previousPanel.gameObject.name}");
        }
        else
        {
            ExitGame();
        }
    }

    private System.Collections.IEnumerator SelectButtonDelayed(Button buttonToSelect)
    {
        EventSystem.current.SetSelectedGameObject(null);
        yield return null;
        EventSystem.current.SetSelectedGameObject(buttonToSelect.gameObject);
    }
}