using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    [Header("Eventy i Status")]
    [Tooltip("Wywoływane, gdy gracz WŁAŚNIE wszedł w pole widzenia i jest widoczny.")]
    public UnityEvent OnPlayerDetected;

    [Tooltip("Wywoływane, gdy wykryty gracz WŁAŚNIE zniknął z pola widzenia.")]
    public UnityEvent OnPlayerLost;

    [SerializeField]
    [Tooltip("Aktualnie wykryty i widoczny gracz. Pojawi się tutaj.")]
    private PlayerController detectedPlayer;
    private bool wasPlayerDetectedLastFrame = false;

    // --- Publiczna właściwość (Property) ---
    public PlayerController DetectedPlayer => detectedPlayer;

    private void Update()
    {
        FindVisiblePlayer();

        if (detectedPlayer != null && !wasPlayerDetectedLastFrame)
        {
            OnPlayerDetected.Invoke();
            wasPlayerDetectedLastFrame = true;
        }
        else if (detectedPlayer == null && wasPlayerDetectedLastFrame)
        {
            OnPlayerLost.Invoke();
            wasPlayerDetectedLastFrame = false;
        }
    }

    private void FindVisiblePlayer()
    {
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
                if (!Physics.Raycast(transform.position, dirToPlayer, distanceToPlayer, obstacleMask))
                {
                    // ...to znaczy, że gracz jest widoczny!
                    detectedPlayer = playerCollider.GetComponent<PlayerController>();
                    return;
                }
            }
        }
    }

    // --- Helpery i Gizmos (bez zmian) ---
    private Vector3 DirFromAngle(float angleInDegrees)
    {
        angleInDegrees += transform.eulerAngles.y;

        return new Vector3(
            Mathf.Sin(angleInDegrees * Mathf.Deg2Rad),
            0,
            Mathf.Cos(angleInDegrees * Mathf.Deg2Rad)
        );
    }

    private void OnDrawGizmos()
    {
        if (detectedPlayer != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, detectedPlayer.transform.position);
        }
        else
        {
            Gizmos.color = Color.yellow;
        }

        Vector3 viewAngleA = DirFromAngle(-fovAngle / 2);
        Vector3 viewAngleB = DirFromAngle(fovAngle / 2);

        Gizmos.DrawLine(transform.position, transform.position + viewAngleA * fovRadius);
        Gizmos.DrawLine(transform.position, transform.position + viewAngleB * fovRadius);
    }
}