using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// UPROSZCZONA WERSJA: Klasa Singleton zarządzająca stanem punktów i UI.
/// BotPatrol będzie się odwoływał do tej instancji bezpośrednio.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // Singleton
    public static ScoreManager Instance { get; private set; }

    // --- POLA DLA IKONEK HP I ANIMACJI ---
    [Header("Ikony HP Graczy")]
    [Tooltip("Lista obrazków ikon HP dla Gracza 1. Indeks 0 to pierwsze HP, itd.")]
    [SerializeField] private List<Image> hpIconsP1;
    [Tooltip("Lista obrazków ikon HP dla Gracza 2. Indeks 0 to pierwsze HP, itd.")]
    [SerializeField] private List<Image> hpIconsP2;
    [Tooltip("Czas trwania animacji znikania ikonki HP.")]
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("Współczynnik powiększenia ikonki HP podczas animacji znikania.")]
    [SerializeField] private float scaleMultiplier = 1.5f;
    // --------------------------------------------------------

    [Header("Limit Wpadek (HP)")]
    [Tooltip("Maksymalna liczba resetów (wpadek) przed przegraną. MUSI BYĆ WIĘKSZE NIŻ 0 (np. 2).")]
    [SerializeField] private int maxFails = 2; // WAŻNE: Ustaw domyślnie na 2

    [Header("Stan Wpadek Graczy")]
    private int failsP1 = 0;
    private int failsP2 = 0;

    [Header("Punkty Graczy")]
    private int scoreP1 = 0;
    private int scoreP2 = 0;

    [Header("Komponenty UI - Liczniki Punktów")]
    [Tooltip("Text Mesh Pro dla punktacji Gracza 1")]
    [SerializeField] private TMP_Text scoreTextP1;
    [Tooltip("Text Mesh Pro dla punktacji Gracza 2")]
    [SerializeField] private TMP_Text scoreTextP2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            Debug.Log("[ScoreManager] Singleton zainicjowany.");

            UpdateScoreUI(1);
            UpdateScoreUI(2);
            ResetHpIcons();

            if (maxFails <= 0)
            {
                Debug.LogError("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Debug.LogError("[ScoreManager] KRYTYCZNY BŁĄD: 'Max Fails' jest ustawione na 0. Proszę ustawić na 2 lub więcej w Inspektorze.");
                Debug.LogError("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }
        }
    }

    // --- USUNIĘTO SetBotPatrolReference i OnBotCaughtPlayer ---

    public void AddPoints(int playerID, int points)
    {
        if (playerID == 1)
        {
            scoreP1 += points;
            UpdateScoreUI(1);
        }
        else if (playerID == 2)
        {
            scoreP2 += points;
            UpdateScoreUI(2);
        }
    }

    /// <summary>
    /// NOWA METODA: Wywoływana przez Bota, gdy złapie gracza.
    /// Rejestruje wpadkę, resetuje punkty i ZWRACA (return) true, jeśli to Game Over.
    /// </summary>
    /// <returns>True jeśli Game Over, False jeśli tylko Soft Attack.</returns>
    public bool RecordPlayerFail(int playerID)
    {
        int currentFails = 0;

        if (playerID == 1)
        {
            scoreP1 = 0;
            if (scoreTextP1 != null) scoreTextP1.text = scoreP1.ToString();
            failsP1++;
            currentFails = failsP1;
            AnimateHpLoss(hpIconsP1, failsP1);
        }
        else if (playerID == 2)
        {
            scoreP2 = 0;
            if (scoreTextP2 != null) scoreTextP2.text = scoreP2.ToString();
            failsP2++;
            currentFails = failsP2;
            AnimateHpLoss(hpIconsP2, failsP2);
        }

        Debug.Log($"[ScoreManager] Gracz {playerID}: Nowy stan wpadek: {currentFails}/{maxFails}.");

        // Sprawdzenie Game Over
        if (currentFails >= maxFails)
        {
            Debug.Log($"🛑 [ScoreManager] GRACZ {playerID} PRZEGRYWA! Zwracam TRUE (Game Over).");
            return true; // Tak, to jest Game Over
        }
        else
        {
            Debug.Log($"[ScoreManager] Gracz {playerID} złapany. Zwracam FALSE (Soft Attack).");
            return false; // Nie, to tylko Soft Attack
        }
    }

    private void UpdateScoreUI(int playerID)
    {
        if (playerID == 1 && scoreTextP1 != null)
        {
            scoreTextP1.text = scoreP1.ToString();
        }
        else if (playerID == 2 && scoreTextP2 != null)
        {
            scoreTextP2.text = scoreP2.ToString();
        }
    }

    // --- USUNIĘTO CheckForGameOver (logika przeniesiona do RecordPlayerFail) ---

    private void AnimateHpLoss(List<Image> hpIcons, int currentFailsCount)
    {
        if (hpIcons == null || hpIcons.Count == 0 || currentFailsCount <= 0 || currentFailsCount > hpIcons.Count)
        {
            return;
        }

        Image iconToFade = hpIcons[hpIcons.Count - currentFailsCount];

        if (iconToFade != null)
        {
            iconToFade.DOKill(true);
            Vector3 originalScale = iconToFade.transform.localScale;
            iconToFade.transform.DOScale(originalScale * scaleMultiplier, fadeDuration * 0.5f)
                .OnComplete(() => {
                    iconToFade.DOFade(0f, fadeDuration * 0.5f);
                    iconToFade.transform.DOScale(Vector3.zero, fadeDuration * 0.5f)
                        .SetEase(Ease.InBack);
                });
        }
    }

    public void ResetHpIcons()
    {
        ResetHpIconsForPlayer(hpIconsP1);
        ResetHpIconsForPlayer(hpIconsP2);
        failsP1 = 0;
        failsP2 = 0;
    }

    private void ResetHpIconsForPlayer(List<Image> hpIcons)
    {
        if (hpIcons != null)
        {
            foreach (Image icon in hpIcons)
            {
                if (icon != null)
                {
                    icon.DOKill(true);
                    icon.gameObject.SetActive(true);
                    icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 1f);
                    icon.transform.localScale = Vector3.one;
                }
            }
        }
    }
}