using UnityEngine;

public class InteractionPoint : MonoBehaviour
{
    // Maksymalna liczba studentów, którzy mogą naraz używać tego punktu (np. 1 dla ławki, 4 dla stolika)
    public int MaxUsers = 1;

    // Liczba studentów, którzy aktualnie używają tego punktu
    [HideInInspector]
    public int CurrentUsers = 0;

    // Typ interakcji, która ma być wykonana w tym punkcie
    public enum InteractionType { Sit, GroupChat, IdleAnimation }
    public InteractionType Type = InteractionType.Sit;

    private void Awake()
    {
        // Upewnij się, że collider jest triggerem.
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            // Możesz automatycznie dodać kolider, jeśli go brakuje
            gameObject.AddComponent<BoxCollider>().isTrigger = true;
        }
    }

    private void OnDrawGizmos()
    {
        // Wizualizacja punktu i kierunku w edytorze: Czerwona linia to kierunek patrzenia (Rotacja obiektu).
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1f);
    }
}