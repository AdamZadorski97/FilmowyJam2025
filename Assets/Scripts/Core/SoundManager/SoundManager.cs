using UnityEngine;
using System.Collections.Generic; // Potrzebne do List i Dictionary

public class SoundManager : MonoBehaviour
{
    // Statyczna instancja Singletona.
    public static SoundManager Instance { get; private set; }

    [System.Serializable] // Umożliwia edycję w Inspektorze
    public class SoundEntry
    {
        [Tooltip("Nazwa, po której będziesz wywoływać dźwięk (np. 'ButtonSelect', 'ButtonClick').")]
        public string soundName;
        [Tooltip("Plik dźwiękowy do odtworzenia.")]
        public AudioClip audioClip;
    }

    [Header("Sound Settings")]
    [Tooltip("Lista dla dźwięków interfejsu użytkownika (UI).")]
    public List<SoundEntry> uiSounds = new List<SoundEntry>();
    [Tooltip("Lista dla ogólnych efektów dźwiękowych (SFX).")]
    public List<SoundEntry> sfxSounds = new List<SoundEntry>();
    [Tooltip("Lista dla plików muzycznych w grze.")]
    public List<SoundEntry> musicTracks = new List<SoundEntry>(); // Nowa lista na muzykę

    // Słowniki do szybkiego wyszukiwania AudioClipów po nazwie (optymalizacja wydajności).
    private Dictionary<string, AudioClip> uiSoundDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxSoundDictionary = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> musicDictionary = new Dictionary<string, AudioClip>(); // Nowy słownik na muzykę

    [Header("Audio Sources")]
    [Tooltip("AudioSource dla efektów dźwiękowych i dźwięków UI.")]
    private AudioSource sfxAudioSource;
    [Tooltip("AudioSource dedykowane dla muzyki w tle.")]
    private AudioSource musicAudioSource; // Nowy AudioSource dla muzyki

    private void Awake()
    {
        // Implementacja Singletona: upewnij się, że istnieje tylko jedna instancja.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Zniszcz ten obiekt, jeśli instancja już istnieje.
            return;
        }
        Instance = this; // Ustaw tę instancję jako Singleton.

        // Zapewnij, że ten GameObject nie zostanie zniszczony podczas ładowania nowej sceny.
        DontDestroyOnLoad(gameObject);

        // Dodaj lub znajdź komponent AudioSource dla SFX.
        sfxAudioSource = gameObject.AddComponent<AudioSource>(); // Upewnij się, że zawsze jest dodawany
        sfxAudioSource.playOnAwake = false; // Nie odtwarzaj automatycznie.
        sfxAudioSource.spatialBlend = 0; // Dźwięki 2D.
        sfxAudioSource.loop = false; // SFX zazwyczaj nie zapętla się.

        // Dodaj lub znajdź komponent AudioSource dla Muzyki.
        musicAudioSource = gameObject.AddComponent<AudioSource>(); // Upewnij się, że zawsze jest dodawany
        musicAudioSource.playOnAwake = false; // Nie odtwarzaj automatycznie.
        musicAudioSource.spatialBlend = 0; // Dźwięki 2D.
        musicAudioSource.loop = true; // Muzyka zazwyczaj się zapętla.

        // Wypełnij słowniki dźwięków z list z Inspektora.
        PopulateDictionaries();
    }

    // Wypełnia słowniki dźwięków z list, umożliwiając szybkie wyszukiwanie po nazwie.
    private void PopulateDictionaries()
    {
        uiSoundDictionary.Clear();
        foreach (SoundEntry entry in uiSounds)
        {
            if (uiSoundDictionary.ContainsKey(entry.soundName))
            {
                Debug.LogWarning($"SoundManager: Znaleziono zduplikowaną nazwę dźwięku UI '{entry.soundName}'. Użyta zostanie tylko pierwsza definicja.");
                continue;
            }
            uiSoundDictionary.Add(entry.soundName, entry.audioClip);
        }

        sfxSoundDictionary.Clear();
        foreach (SoundEntry entry in sfxSounds)
        {
            if (sfxSoundDictionary.ContainsKey(entry.soundName))
            {
                Debug.LogWarning($"SoundManager: Znaleziono zduplikowaną nazwę dźwięku SFX '{entry.soundName}'. Użyta zostanie tylko pierwsza definicja.");
                continue;
            }
            sfxSoundDictionary.Add(entry.soundName, entry.audioClip);
        }

        musicDictionary.Clear();
        foreach (SoundEntry entry in musicTracks) // Wypełnij słownik muzyki
        {
            if (musicDictionary.ContainsKey(entry.soundName))
            {
                Debug.LogWarning($"SoundManager: Znaleziono zduplikowaną nazwę utworu muzycznego '{entry.soundName}'. Użyta zostanie tylko pierwsza definicja.");
                continue;
            }
            musicDictionary.Add(entry.soundName, entry.audioClip);
        }
    }

    /// <summary>
    /// Odtwarza dźwięk UI o podanej nazwie.
    /// </summary>
    /// <param name="soundName">Nazwa dźwięku UI do odtworzenia (np. "ButtonSelect").</param>
    public void PlayUISound(string soundName)
    {
        if (uiSoundDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            if (clip != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(clip);
            }
            else if (clip == null)
            {
                Debug.LogWarning($"SoundManager: Plik audio dla dźwięku UI '{soundName}' jest pusty.");
            }
        }
        else
        {
            Debug.LogWarning($"SoundManager: Dźwięk UI o nazwie '{soundName}' nie został znaleziony w słowniku.");
        }
    }

    /// <summary>
    /// Odtwarza efekt dźwiękowy (SFX) o podanej nazwie.
    /// </summary>
    /// <param name="soundName">Nazwa efektu dźwiękowego do odtworzenia (np. "PlayerHit").</param>
    public void PlaySFX(string soundName)
    {
        if (sfxSoundDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            if (clip != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(clip);
            }
            else if (clip == null)
            {
                Debug.LogWarning($"SoundManager: Plik audio dla SFX '{soundName}' jest pusty.");
            }
        }
        else
        {
            Debug.LogWarning($"SoundManager: SFX o nazwie '{soundName}' nie został znaleziony w słowniku.");
        }
    }
    private void Start()
    {
        PlayMusic("tło");
    }
    /// <summary>
    /// Odtwarza utwór muzyczny o podanej nazwie.
    /// Jeśli utwór jest już odtwarzany, nic nie robi.
    /// </summary>
    /// <param name="musicName">Nazwa utworu muzycznego do odtworzenia.</param>
    public void PlayMusic(string musicName)
    {
        if (musicAudioSource == null) return;

        if (musicDictionary.TryGetValue(musicName, out AudioClip clip))
        {
            if (clip != null)
            {
                // Jeśli ten sam utwór już gra, nie odtwarzaj go ponownie.
                if (musicAudioSource.clip == clip && musicAudioSource.isPlaying)
                {
                    Debug.Log($"SoundManager: Muzyka '{musicName}' już gra.");
                    return;
                }

                musicAudioSource.clip = clip;
                musicAudioSource.Play();
                musicAudioSource.volume = 0.25f;
                Debug.Log($"SoundManager: Odtwarzam muzykę: '{musicName}'.");
            }
            else
            {
                Debug.LogWarning($"SoundManager: Plik audio dla muzyki '{musicName}' jest pusty.");
            }
        }
        else
        {
            Debug.LogWarning($"SoundManager: Muzyka o nazwie '{musicName}' nie została znaleziona w słowniku.");
        }
    }

    /// <summary>
    /// Zatrzymuje aktualnie odtwarzany utwór muzyczny.
    /// </summary>
    public void StopMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
            Debug.Log("SoundManager: Muzyka zatrzymana.");
        }
    }

    /// <summary>
    /// Pauzuje aktualnie odtwarzany utwór muzyczny.
    /// </summary>
    public void PauseMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause();
            Debug.Log("SoundManager: Muzyka spauzowana.");
        }
    }

    /// <summary>
    /// Wznawia odtwarzanie spauzowanego utworu muzycznego.
    /// </summary>
    public void ResumeMusic()
    {
        if (musicAudioSource != null && !musicAudioSource.isPlaying && musicAudioSource.time > 0)
        {
            musicAudioSource.UnPause();
            Debug.Log("SoundManager: Muzyka wznowiona.");
        }
    }

    /// <summary>
    /// Ustawia głośność muzyki.
    /// </summary>
    /// <param name="volume">Wartość głośności (0.0 do 1.0).</param>
    public void SetMusicVolume(float volume)
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = Mathf.Clamp01(volume);
        }
    }

    /// <summary>
    /// Ustawia głośność efektów dźwiękowych (SFX i UI).
    /// </summary>
    /// <param name="volume">Wartość głośności (0.0 do 1.0).</param>
    public void SetSFXVolume(float volume)
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = Mathf.Clamp01(volume);
        }
    }

    // Wywoływane w edytorze Unity, gdy zmienia się wartości w Inspektorze.
    void OnValidate()
    {
        // Wypełnij słowniki ponownie, gdy zmieniasz coś w Inspektorze w trybie edycji.
        if (!Application.isPlaying)
        {
            PopulateDictionaries();
        }
    }
}