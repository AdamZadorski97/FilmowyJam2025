using Sirenix.OdinInspector;
using UnityEngine;

public class HintManagerInitializer : MonoBehaviour
{
    [Header("Zasoby dla Hint Managera")]
    [SerializeField] private BindingProperties bindingProperties;
    [SerializeField] private GameObject hintPrefab;

    private void Awake()
    {
        // Przekaż referencje do statycznego managera
        InputHintController.Init(bindingProperties, hintPrefab);
    }
    [Button]
   void SpawnHintTest(string actionName, string message)
    {
        InputHintController.Show(actionName, message);
    }
    [Button]
    void HideTest()
    {
        InputHintController.Hide();
    }
}