using UnityEngine;

public class KebabController : MonoBehaviour
{
    [SerializeField] private GameObject KebabPrefab;


    public void Kebabownia()
    {
        KebabPrefab.SetActive(true);
        gameObject.SetActive(false);
    }

}
