using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Klasa Singleton zarządzająca faktycznym stanem punktów i UI.
/// MUSI być dołączona do obiektu w scenie (np. na głównym Canvasie).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // Singleton, który jest wymagany przez ScoreService
    public static ScoreManager Instance { get; private set; }

    // --- REFERENCJA I SUBKRYPCJA ---
    private static BotPatrol botPatrolReference;

    public static void SetBotPatrolReference(BotPatrol bot)
    {
        botPatrolReference = bot;

        // Podpinamy event złapania, jeśli ScoreManager już istnieje
        if (Instance != null && bot != null)
        {
            botPatrolReference.OnPlayerCaught.AddListener(Instance.OnBotCaughtPlayer);
        }
    }

    private void OnBotCaughtPlayer(PlayerController player)
    {
        // Ta metoda jest wywoływana przez BotPatrol.OnPlayerCaught (po cooldownie bota)
        if (player != null)
        {
            // UWAGA: Zakładamy, że PlayerController ma publiczne pole int playerID.
            ResetPoints(player.playerID);
        }
    }
    // ------------------------------------------------

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
    [Tooltip("Maksymalna liczba resetów (wpadek) przed przegraną.")]
    [SerializeField] private int maxFails = 2;

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

            // JEŚLI REFERENCJA DO BOTA ISTNIEJE JUŻ PRZED AWAKE, podpinamy event.
            if (botPatrolReference != null)
            {
                botPatrolReference.OnPlayerCaught.AddListener(OnBotCaughtPlayer);
            }

            UpdateScoreUI(1);
            UpdateScoreUI(2);
            ResetHpIcons();
        }
    }

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
    /// Resetuje punkty gracza, inkrementuje licznik wpadek (fails) i animuje zniknięcie ikonki HP.
    /// </summary>
    public void ResetPoints(int playerID)
    {
        if (playerID == 1)
        {
            scoreP1 = 0;
            if (scoreTextP1 != null) scoreTextP1.text = scoreP1.ToString();

            failsP1++;

            AnimateHpLoss(hpIconsP1, failsP1);

            CheckForGameOver(playerID, failsP1);
        }
        else if (playerID == 2)
        {
            scoreP2 = 0;
            if (scoreTextP2 != null) scoreTextP2.text = scoreP2.ToString();

            failsP2++;

            AnimateHpLoss(hpIconsP2, failsP2);

            CheckForGameOver(playerID, failsP2);
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

    /// <summary>
    /// Sprawdza, czy gracz osiągnął limit wpadek. Wywołuje finalną animację.
    /// </summary>
    private void CheckForGameOver(int playerID, int currentFails)
    {
        if (currentFails >= maxFails)
        {
            Debug.Log($"🛑 GRACZ {playerID} PRZEGRAŁ! Osiągnięto limit wpadek ({maxFails}).");

            // WYWOŁANIE FINALNEGO ATAKU NUCZYCIELA
            if (botPatrolReference != null)
            {
                botPatrolReference.TriggerFinalAttack();
            }

        }
        else
        {
            Debug.Log($"Gracz {playerID} został przyłapany! Wpadka {currentFails}/{maxFails}.");
        }
    }

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