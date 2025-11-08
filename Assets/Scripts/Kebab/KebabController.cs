using UnityEngine;

public class KebabController : MonoBehaviour
{
    [SerializeField] private GameObject KebabPrefab;
    [SerializeField] private GameObject UnkebabPrefab;


    public void Kebabownia()
    {
        UnkebabPrefab.SetActive(false);
        KebabPrefab.SetActive(true);
    }

}
