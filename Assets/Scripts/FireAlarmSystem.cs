using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.VFX;

[RequireComponent(typeof(AudioSource))]
public class FireAlarmSystem : MonoBehaviour
{
    // 1. Implementacja Wzoru Singleton
    public static FireAlarmSystem Instance { get; private set; }

    [Header("Światła Alarmowe (Dzieci Rodzica)")]
    [Tooltip("Rodzic zawierający wszystkie komponenty Light, które mają mrugać.")]
    [SerializeField] private GameObject lightsParent;
    [SerializeField] private float maxIntensity = 5f;
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private int flashRepeats = 2;

    [Header("Efekty Wizualne (Dzieci Rodzica)")]
    [Tooltip("Rodzic zawierający wszystkie komponenty VisualEffect, które mają być włączane/wyłączane.")]
    [SerializeField] private GameObject vfxParent;

    [Header("Ustawienia Audio")]
    [Tooltip("Dźwięk syreny, zapętlony podczas działania alarmu (przez AudioSource na tym obiekcie).")]
    [SerializeField] private AudioClip loopAlarmSound;

    [Tooltip("Krótki dźwięk odtwarzany raz przy włączaniu alarmu.")]
    [SerializeField] private AudioClip startAlarmSoundOneShot;

    [Tooltip("Krótki dźwięk odtwarzany raz przy wyłączaniu alarmu.")]
    [SerializeField] private AudioClip stopAlarmSoundOneShot;
    public Color red;
    public Color white;
    // Prywatne zmienne
    private List<Light> lights = new List<Light>();
    private List<VisualEffect> visualEffects = new List<VisualEffect>();
    private AudioSource audioSource;
    private Sequence lightSequence;
    private bool isAlarmActive = false;

    // Właściwość publiczna do sprawdzania stanu
    public bool IsAlarmActive => isAlarmActive;


    void Awake()
    {
        // Sprawdzenie i ustawienie Singletonu
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            InitializeSystem();
        }
    }

    private void InitializeSystem()
    {
        // 1. Pobieranie Świateł
        if (lightsParent != null)
        {
            lights.AddRange(lightsParent.GetComponentsInChildren<Light>());
        }

        // 2. Pobieranie Efektów VFX
        if (vfxParent != null)
        {
            visualEffects.AddRange(vfxParent.GetComponentsInChildren<VisualEffect>());
        }

        // 3. Konfiguracja AudioSource
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        // Dźwięk pętli ustawiamy raz, ale nie uruchamiamy
        audioSource.clip = loopAlarmSound;

        // Upewnij się, że system jest wyłączony na starcie
        SetLightsState(1f);
        SetVFXState(false);
    }

    // ---------------------------------------------
    // NOWA FUNKCJA: Przełączanie Stanu Alarmu
    // ---------------------------------------------

    /// <summary>
    /// Włącza alarm, jeśli jest wyłączony, lub wyłącza, jeśli jest włączony.
    /// </summary>
    public void ToggleAlarm()
    {
        if (isAlarmActive)
        {
            StopAlarm();
        }
        else
        {
            StartAlarm();
        }
    }

    // ---------------------------------------------
    // Funkcje Włączania/Wyłączania
    // ---------------------------------------------

    public void StartAlarm()
    {
        if (isAlarmActive) return;

        foreach (var light in lights)
        {
            light.color = red;
        }
            isAlarmActive = true;

        // Jednorazowy dźwięk WŁĄCZENIA
        if (startAlarmSoundOneShot != null)
        {
            audioSource.PlayOneShot(startAlarmSoundOneShot);
        }

        // Audio w Pętli
        if (audioSource.clip != null)
        {
            // Uruchamiamy dźwięk pętli po krótkim opóźnieniu, aby PlayOneShot miał szansę się odtworzyć
            audioSource.PlayDelayed(0.1f);
        }

        // Światła (Mruganie)
        StartFlickerSequence();

        // VFX (Włączenie Emiterów)
        SetVFXState(true);

        Debug.Log("Alarm Pożarowy WŁĄCZONY.");
    }

    public void StopAlarm()
    {
        if (!isAlarmActive) return;

        isAlarmActive = false;
        foreach (var light in lights)
        {
            light.color = white;
        }
        // Audio
        audioSource.Stop();

        // Jednorazowy dźwięk WYŁĄCZENIA
        if (stopAlarmSoundOneShot != null)
        {
            audioSource.PlayOneShot(stopAlarmSoundOneShot);
        }

        // Światła (Zakończenie i wyłączenie)
        lightSequence?.Kill(true);
        SetLightsState(1f);

        // VFX (Wyłączenie Emiterów)
        SetVFXState(false);

        Debug.Log("Alarm Pożarowy WYŁĄCZONY.");
    }

    // ---------------------------------------------
    // Funkcje Pomocnicze (bez zmian)
    // ---------------------------------------------

    private void SetLightsState(float intensity)
    {
        foreach (var light in lights)
        {
            light.intensity = intensity;
            light.enabled = intensity > 0f;
        }
    }

    private void SetVFXState(bool state)
    {
        foreach (var vfx in visualEffects)
        {
            if (state)
            {
                vfx.Play();
            }
            else
            {
                vfx.Stop();
            }
        }
    }

    private void StartFlickerSequence()
    {
        lightSequence?.Kill();
        SetLightsState(minIntensity);

        lightSequence = DOTween.Sequence().SetAutoKill(false).SetLoops(-1, LoopType.Restart);

        foreach (var light in lights)
        {
            lightSequence.Join(light.DOIntensity(maxIntensity, flashDuration / (flashRepeats * 2))
                .SetEase(Ease.OutSine)
                .SetLoops(flashRepeats * 2, LoopType.Yoyo)
                .SetDelay(Random.Range(0f, flashDuration * flashRepeats))
            );
        }

        lightSequence.Play();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        lightSequence?.Kill();
    }
}