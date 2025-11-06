using UnityEngine;

public class PlayerFogRevealer : MonoBehaviour
{
    public ProceduralSphere fogSphere; // Referencja do skryptu sfery
    public float updateInterval = 0.05f; // Jak często gracz wysyła pozycję do sfery (co ile sekund)

    private float timer;

    void Start()
    {
        if (fogSphere == null)
        {
            fogSphere = FindObjectOfType<ProceduralSphere>();
            if (fogSphere == null)
            {
                Debug.LogError("Błąd: Skrypt ProceduralSphere nie został znaleziony w scenie! Przypisz go ręcznie.");
                enabled = false;
                return;
            }
        }
        timer = updateInterval;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (fogSphere != null)
            {
                fogSphere.RevealArea(transform.position);
            }
            timer = updateInterval;
        }
    }
}