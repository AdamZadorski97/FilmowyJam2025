// PlanetEditor.cs
using UnityEngine;

public class PlanetEditor : MonoBehaviour
{
    public SphereGenerator sphereGenerator;
    public float deformationRadius = 5f; // Promień wpływu deformacji
    public float deformationStrength = 1f; // Siła deformacji (jak bardzo wierzchołki się podnoszą)

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 1f); // Rysuje czerwony promień na 1 sekundę

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Hit: " + hit.collider.name + " at " + hit.point); // Wypisuje w konsoli, co zostało trafione

                if (hit.collider.gameObject == sphereGenerator.gameObject)
                {
                    sphereGenerator.DeformSphere(hit.point, deformationRadius, deformationStrength);
                }
            }
            else
            {
                Debug.Log("No hit."); // Wypisuje, jeśli nic nie zostało trafione
            }
        }
    }
}