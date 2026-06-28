using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Serializable]
    public class SoundData
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop;
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio List")]
    [SerializeField] private List<SoundData> musicList = new List<SoundData>();
    [SerializeField] private List<SoundData> sfxList = new List<SoundData>();

    private readonly HashSet<Button> registeredButtons = new HashSet<Button>();
    private string currentMusicName;

    public float MusicVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;

    public bool IsMusicMuted { get; private set; }
    public bool IsSFXMuted { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        foreach (Button button in registeredButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(PlayButtonClickSFX);
        }

        registeredButtons.Clear();
        Instance = null;
    }

    public void PlayMusic(MusicName musicName)
    {
        PlayMusic(musicName.ToString());
    }

    public void PlaySFX(SFXName sfxName)
    {
        PlaySFX(sfxName.ToString());
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || IsSFXMuted)
            return;

        sfxSource.PlayOneShot(clip, SFXVolume);
    }

    public void PlayMusic(string musicName)
    {
        SoundData music = musicList.Find(x => x.name == musicName);

        if (music == null || music.clip == null)
        {
            Debug.LogWarning($"Music '{musicName}' not found.");
            return;
        }

        if (currentMusicName == musicName && musicSource.isPlaying)
            return;

        currentMusicName = musicName;
        musicSource.clip = music.clip;
        musicSource.loop = music.loop;
        musicSource.volume = IsMusicMuted ? 0f : music.volume * MusicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySFX(string sfxName)
    {
        SoundData sfx = sfxList.Find(x => x.name == sfxName);

        if (sfx == null || sfx.clip == null)
        {
            Debug.LogWarning($"SFX '{sfxName}' not found.");
            return;
        }

        if (IsSFXMuted) return;

        sfxSource.PlayOneShot(sfx.clip, sfx.volume * SFXVolume);
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = value;

        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.volume = IsMusicMuted ? 0f : MusicVolume;
        }
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = value;
    }

    public void ToggleMusicMute()
    {
        IsMusicMuted = !IsMusicMuted;

        if (musicSource != null)
        {
            musicSource.volume = IsMusicMuted ? 0f : MusicVolume;
        }
    }

    public void ToggleSFXMute()
    {
        IsSFXMuted = !IsSFXMuted;
    }

    public void RegisterButton(Button button)
    {
        if (button == null || !registeredButtons.Add(button))
            return;

        button.onClick.AddListener(PlayButtonClickSFX);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "Gameplay":
            case "Main Menu":
                PlayMusic(MusicName.Gameplay);
                break;
            case "Battle":
                PlayMusic(MusicName.Battle);
                break;
            case "Interior":
                PlayMusic(MusicName.Interior);
                break;
        }

        RegisterSceneButtons();
    }

    private void RegisterSceneButtons()
    {
        registeredButtons.RemoveWhere(button => button == null);

        Button[] buttons = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Button button in buttons)
            RegisterButton(button);
    }

    private void PlayButtonClickSFX()
    {
        if (SceneManager.GetActiveScene().name == "Battle")
            return;

        PlaySFX(SFXName.Click);
    }
}
