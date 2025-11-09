using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class BoardController : MonoBehaviour
{
    // Pole publiczne do przypisania w Inspektorze:
    [Tooltip("MeshRenderer obiektu, z którego pobierzemy i sklonujemy materiał.")]
    public MeshRenderer targetRenderer;

    // Prywatna referencja do instancji materiału, na której będziemy pracować.
    private Material instancedMaterial;

    [Header("Parametry Animacji")]
    public float duration = 2.0f;
    public Ease easeType = Ease.OutQuad;

    private const string ProgressPropertyName = "_Progress";

    void Awake()
    {
        if (targetRenderer == null)
        {
            // Spróbuj pobrać MeshRenderer z tego samego obiektu, jeśli nie został przypisany.
            targetRenderer = GetComponent<MeshRenderer>();
        }

        if (targetRenderer != null)
        {
            // !!! KLUCZOWA ZMIANA !!! 
            // Tworzymy instancję materiału: .material tworzy kopię materiału 
            // i przypisuje ją do tego konkretnego renderera.
            instancedMaterial = targetRenderer.material;

            // Ustawienie początkowej wartości na 0.
            instancedMaterial.SetFloat(ProgressPropertyName, 0);
            Debug.Log("Utworzono instancję materiału i przypisano do BoardController.");
        }
        else
        {
            Debug.LogError("Brak MeshRenderer na obiekcie lub w polu Target Renderer!");
        }
    }

    /// <summary>
    /// Płynnie animuje wartość _Progress w sklonowanym shaderze z 0 do 1.
    /// </summary>
    [Button]
    public void Draw()
    {
        if (instancedMaterial == null) return;

        Debug.Log("Rozpoczynanie animacji rysowania...");

        // Zakończ ewentualną poprzednią animację na tym materiale.
        instancedMaterial.DOKill();

        // Użycie DOTween do animacji float na instancji materiału
        instancedMaterial.DOFloat(1f, ProgressPropertyName, duration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                Debug.Log("Animacja rysowania zakończona.");
            });

        UIcontrollerPopUp.Instance.IncraseRep();
    }

    /// <summary>
    /// Opcjonalna funkcja cofająca animację.
    /// </summary>
    public void Undraw()
    {
        if (instancedMaterial == null) return;

        instancedMaterial.DOKill();

        // Anulowanie animacji: zmiana wartości z 1 na 0
        instancedMaterial.DOFloat(0f, ProgressPropertyName, duration)
            .SetEase(easeType);
    }
}