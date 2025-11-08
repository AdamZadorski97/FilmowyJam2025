using UnityEngine;
using TMPro;

/// <summary>
/// Klasa Singleton zarządzająca faktycznym stanem punktów i UI.
/// MUSI być dołączona do obiektu w scenie (np. na głównym Canvasie).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // Singleton, który jest wymagany przez ScoreService
    public static ScoreManager Instance { get; private set; }

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
            // Inicjalizacja liczników w UI
            UpdateScoreUI(1);
            UpdateScoreUI(2);
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
}