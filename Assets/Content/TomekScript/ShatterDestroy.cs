using UnityEngine;

public class ShatterDestroy : MonoBehaviour
{
    public GameObject ShatterVersion;
    public Vector3 Offset;



    public void DestroyIt()
    {
        Vector3 PositionFor = transform.position + Offset;
        GameObject ShatterClone = Instantiate(ShatterVersion, PositionFor, transform.rotation);
        gameObject.SetActive(false);

    }
}
