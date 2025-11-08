using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Odpowiada za pole widzenia (FOV) nauczyciela.
/// Wykrywa graczy w zasięgu, kącie widzenia i sprawdza linię wzroku (LOS).
/// </summary>
public class TeacherFOV : MonoBehaviour
{
    [Header("Ustawienia Pola Widzenia")]
    [Tooltip("Jak daleko nauczyciel widzi.")]
    public float fovRadius = 10f;

    [Range(0, 360)]
    [Tooltip("Szerokość kąta widzenia nauczyciela (w stopniach).")]
    public float fovAngle = 90f;

    [Header("Konfiguracja Wykrywania")]
    [Tooltip("Warstwa (Layer), na której znajdują się gracze.")]
    public LayerMask playerMask;

    [Tooltip("Warstwy (Layers), które blokują wzrok (np. ściany, meble).")]
    public LayerMask obstacleMask;

    [Header("Status (Tylko do odczytu)")]
    [SerializeField]
    [Tooltip("Aktualnie wykryty i widoczny gracz. Pojawi się tutaj.")]
    private PlayerController detectedPlayer;

    // --- Publiczna właściwość (Property) ---
    /// <summary>
    /// Zwraca aktualnie widocznego gracza lub null, jeśli nikt nie jest widoczny.
    /// </summary>
    public PlayerController DetectedPlayer => detectedPlayer;

    private void Update()
    {
        // W każdej klatce uruchom logikę znajdowania gracza
        FindVisiblePlayer();
    }

    private void FindVisiblePlayer()
    {
        // 1. Zresetuj cel. Domyślnie nikogo nie widzimy.
        detectedPlayer = null;

        // 2. Znajdź wszystkich graczy w zasięgu (kula)
        Collider[] playersInRadius = Physics.OverlapSphere(transform.position, fovRadius, playerMask);

        // 3. Przejrzyj każdego znalezionego gracza
        foreach (Collider playerCollider in playersInRadius)
        {
            Transform playerTransform = playerCollider.transform;
            Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;

            // 4. Sprawdź, czy gracz jest w KĄCIE widzenia
            if (Vector3.Angle(transform.forward, dirToPlayer) < fovAngle / 2)
            {
                // 5. Sprawdź LINIĘ WZROKU (czy nie ma przeszkód)
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

                // Wystrzel promień OD NAUCZYCIELA DO GRACZA
                // Jeśli NIE trafi on w przeszkodę (obstacleMask)...
                if (!Physics.Raycast(transform.position, dirToPlayer, distanceToPlayer, obstacleMask))
                {
                    // ...to znaczy, że gracz jest widoczny!
                    detectedPlayer = playerCollider.GetComponent<PlayerController>();

                    // Znaleźliśmy gracza, możemy przerwać pętlę i zakończyć funkcję
                    return;
                }
                // Jeśli raycast trafił w przeszkodę, ignorujemy tego gracza (pętla szuka dalej)
            }
            // Jeśli gracz nie jest w kącie, ignorujemy go (pętla szuka dalej)
        }

        // Jeśli pętla się zakończyła, a my nic nie zwróciliśmy,
        // 'detectedPlayer' pozostanie 'null', co jest poprawne.
    }

    /// <summary>
    /// Helper do rysowania Gizmos w Edytorze.
    /// </summary>
    private Vector3 DirFromAngle(float angleInDegrees)
    {
        // Dodaj rotację samego obiektu do kąta
        angleInDegrees += transform.eulerAngles.y;

        // Konwertuj kąt na wektor
        return new Vector3(
            Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),
            0,
            Mathf.Cos(angleInDegrees * Mathf.Deg2Rad)
        );
    }

    /// <summary>
    /// Rysuje pole widzenia w edytorze sceny.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Ustaw kolor Gizmo
        if (detectedPlayer != null)
        {
            Gizmos.color = Color.red; // Czerwony, jeśli kogoś widzimy
            Gizmos.DrawLine(transform.position, detectedPlayer.transform.position); // Linia do celu
        }
        else
        {
            Gizmos.color = Color.yellow; // Żółty, jeśli pole jest "czyste"
        }

        // Narysuj krawędzie stożka widzenia
        Vector3 viewAngleA = DirFromAngle(-fovAngle / 2);
        Vector3 viewAngleB = DirFromAngle(fovAngle / 2);

        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * fovRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * fovRadius);
    }
}