using UnityEngine;

public class ShatterDestroy : MonoBehaviour
{
    public GameObject ShatterVersion;
    public Vector3 Offset;



    public void DestroyIt()
    {
        if (Random.Range(0f, 100f) <= 60)
        {
            Vector3 PositionFor = transform.position + Offset;
            GameObject ShatterClone = Instantiate(ShatterVersion, PositionFor, transform.rotation);
            ShatterClone.GetComponent<ShatterObject>().ExplodeChildren();
            gameObject.SetActive(false);
            UIcontrollerPopUp.Instance.UltimatePower();
        }

        

    }
}
