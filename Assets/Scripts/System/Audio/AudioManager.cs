using System;
using System.Collections.Generic;
using UnityEngine;

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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic(MusicName musicName)
    {
        PlayMusic(musicName.ToString());
    }

    public void PlaySFX(SFXName sfxName)
    {
        PlaySFX(sfxName.ToString());
    }

    public void PlayMusic(string musicName)
    {
        SoundData music = musicList.Find(x => x.name == musicName);

        if (music == null || music.clip == null)
        {
            Debug.LogWarning($"Music '{musicName}' not found.");
            return;
        }

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
}