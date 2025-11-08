using UnityEngine;

public class IntoKebab : MonoBehaviour
{
    [SerializeField] private GameObject BagPrefab;

    private void OnEnable()
    {
        Invoke(nameof(UndoKebab), 10f); // wywo³a MyFunction po 10 sekundach
    }

    public void UndoKebab()
    {
        BagPrefab.SetActive(true);
        gameObject.SetActive(false);
    }


}
