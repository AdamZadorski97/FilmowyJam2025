using UnityEngine;

public class DestroyController : MonoBehaviour
{
    // Konfiguracja w Inspektorze
    [Header("Ustawienia fizyki")]
    public float silaPodrzucenia = 5f;
    public float momentObrotowy = 5f;

    // Metoda uruchamiana, gdy chcemy "zniszczyć" obiekt
    public void AktywujDestrukcje()
    {
        transform.SetParent(null);
        // 1. Sprawdzenie i dodanie Collidera (jeśli go nie ma)
        // Obiekt musi mieć Collider, aby Rigidbody działało poprawnie
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Zakładamy, że najczęściej będzie to BoxCollider
            col = gameObject.AddComponent<BoxCollider>();
        }

        // 2. Dodanie komponentu Rigidbody
        // Rigidbody jest potrzebny do symulacji fizycznej (grawitacja, siły, obroty)
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();

        // 3. Zastosowanie siły do podrzucenia obiektu (w górę - Vector3.up - oraz losowo)
        Vector3 losowyKierunek = Random.insideUnitSphere.normalized;
        // Zapewniamy, że dominującym kierunkiem jest GÓRA, mieszając go z losowym
        Vector3 kierunekWypchniecia = (Vector3.up * 1.5f + losowyKierunek).normalized;
        rb.AddForce(kierunekWypchniecia * silaPodrzucenia, ForceMode.Impulse);

        // 4. Zastosowanie losowego momentu obrotowego
        Vector3 losowyMoment = Random.insideUnitSphere * momentObrotowy;
        rb.AddTorque(losowyMoment, ForceMode.Impulse);

        // Opcjonalnie: Usuń skrypt po aktywacji, aby nie zajmował zasobów (chyba, że planujesz go ponownie użyć)
        // Destroy(this); 

        // Możesz także dodać Destroy(gameObject, czas) aby usunąć obiekt po pewnym czasie 
        // np. Destroy(gameObject, 5f);
    }

    // Przykład użycia: możesz wywołać AktywujDestrukcje() z innej klasy, np. gdy kula uderzy w ten obiekt.
}