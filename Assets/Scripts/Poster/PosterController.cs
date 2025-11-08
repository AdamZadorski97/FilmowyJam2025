using UnityEngine;
using DG.Tweening; // Pamiętaj o dodaniu using dla DOTween

public class PosterController : MonoBehaviour
{
    // Czy animacja już się odbyła
    private bool hasTilted = false;

    // Zakresy kątów przechylenia na osi Z
    private const float MinTiltAngle = 15f;
    private const float MaxTiltAngle = 25f;

    // Czas trwania animacji przechylenia
    public float tiltDuration = 0.5f;

    // Funkcja do rozpoczęcia przechylenia
    public void TiltOnce()
    {
        // Sprawdź, czy już się przechyliło
        if (hasTilted)
        {
            return;
        }

        hasTilted = true;

        // Losowo wybierz kąt i kierunek
        float randomAngle = Random.Range(MinTiltAngle, MaxTiltAngle);
        // Zdecyduj, czy kąt ma być dodatni (15 do 25) czy ujemny (-25 do -15)
        bool tiltRight = Random.value > 0.5f;

        float targetZRotation;
        if (tiltRight)
        {
            // Przechylenie w prawo (dodatnie kąty Z)
            targetZRotation = randomAngle;
        }
        else
        {
            // Przechylenie w lewo (ujemne kąty Z)
            targetZRotation = -randomAngle;
        }

        // Pobierz aktualne kąty Eulera
        Vector3 currentRotation = transform.rotation.eulerAngles;
        // Ustaw docelową rotację (zmieniamy tylko oś Z)
        Vector3 targetRotation = new Vector3(targetZRotation, currentRotation.y, currentRotation.z);

        // Użycie DOTween do animacji rotacji
        // SetEase(Ease.OutBack) dodaje ładny "efekt sprężyny"
        transform.DORotate(targetRotation, tiltDuration)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject); // Opcjonalnie: automatycznie zatrzymaj, gdy obiekt zostanie zniszczony
    }

   
}