using UnityEngine;
using System.Collections;
public class UIcontrollerPopUp : MonoBehaviour
{
    // --- IMPLEMENTACJA SINGLETONU ---

    // 1. Statyczna instancja: Umożliwia dostęp z każdego miejsca.
    public static UIcontrollerPopUp Instance { get; private set; }

    private void Awake()
    {
        // 2. Logika sprawdzenia istnienia instancji
        if (Instance != null && Instance != this)
        {
            // Jeśli już istnieje inna instancja, zniszcz ten obiekt.
            Debug.LogWarning($"[UIcontrollerPopUp] Wykryto duplikat. Niszczę obiekt: {gameObject.name}");
            Destroy(this.gameObject);
        }
        else
        {
            // Ustaw ten obiekt jako jedyną instancję.
            Instance = this;
            // Opcjonalnie: Jeśli ten kontroler ma przetrwać ładowanie scen.
            // DontDestroyOnLoad(this.gameObject);
        }
        blackscreenMaterial.SetFloat(ProgressPropertyName, 1);
        fatalityMaterial.SetFloat(ProgressPropertyName, 1);
    }

    // ------------------------------------


    [SerializeField] private Animator UiAnimator;
    [SerializeField] private AudioSource _AudioSource;
    [SerializeField] private AudioClip IncraseRepSound;
    [SerializeField] private AudioClip UltimatePowerSound;

    public Material blackscreenMaterial; // Materiał dla efektu "Blackscreen"
    public Material fatalityMaterial;    // Materiał dla efektu "Fatality"
    private const float LerpDuration = 3f;
    private const string ProgressPropertyName = "_Progress";

    public void IncraseRep()
    {
        UiAnimator.SetTrigger("ReputationIncrase");
        _AudioSource.clip = IncraseRepSound;
        _AudioSource.Play();
    }

    public void UltimatePower()
    {
        UiAnimator.SetTrigger("UltimatePower");
        _AudioSource.clip = UltimatePowerSound;
        _AudioSource.Play();
    }

    public void Fatality()
    {
        if (blackscreenMaterial == null || fatalityMaterial == null)
        {
            Debug.LogError("Brak przypisanych materiałów Blackscreen lub Fatality!");
            return;
        }

        // Zatrzymujemy ewentualne poprzednie animacje, aby uniknąć konfliktów
        StopAllCoroutines();

        // Rozpoczęcie głównej sekwencji
        StartCoroutine(FatalitySequenceCoroutine(blackscreenMaterial, fatalityMaterial));
    }

    /// <summary>
    /// Główna sekwencja Fatality: Animuje _Progress w dwóch materiałach sekwencyjnie.
    /// </summary>
    private IEnumerator FatalitySequenceCoroutine(Material mat1, Material mat2)
    {
        // 1. ANIAMCJA PIERWSZEGO MATERIAŁU (Blackscreen)
        yield return StartCoroutine(AnimateProgress(mat1, 1f, 0f, LerpDuration));

        Debug.Log("Faza Blackscreen zakończona. Przechodzę do Fatality.");

        // Krótka pauza między przejściami (opcjonalna)
        yield return new WaitForSeconds(0.5f);

        // 2. ANIAMCJA DRUGIEGO MATERIAŁU (Fatality)
        yield return StartCoroutine(AnimateProgress(mat2, 1f, 0f, LerpDuration));

        Debug.Log("Sekwencja Fatality zakończona.");
    }

    /// <summary>
    /// Funkcja pomocnicza: Lerpuje właściwość _Progress w danym materiale.
    /// </summary>
    /// <param name="targetMaterial">Materiał do animowania.</param>
    /// <param name="startValue">Wartość początkowa (np. 1f).</param>
    /// <param name="endValue">Wartość końcowa (np. 0f).</param>
    /// <param name="duration">Czas trwania animacji.</param>
    private IEnumerator AnimateProgress(Material targetMaterial, float startValue, float endValue, float duration)
    {
        float startTime = Time.time;
        float elapsedTime = 0f;

        // Ustawienie wartości początkowej
        targetMaterial.SetFloat(ProgressPropertyName, startValue);

        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;

            // Obliczanie współczynnika Lerp (normalized time [0, 1])
            float t = Mathf.Clamp01(elapsedTime / duration);

            // Obliczanie wartości Lerp
            float lerpValue = Mathf.Lerp(startValue, endValue, t);

            // Ustawienie właściwości _Progress w materiale
            targetMaterial.SetFloat(ProgressPropertyName, lerpValue);

            yield return null; // Czekaj jedną klatkę
        }

        // Upewnienie się, że wartość końcowa jest dokładnie ustawiona
        targetMaterial.SetFloat(ProgressPropertyName, endValue);
    }
}

