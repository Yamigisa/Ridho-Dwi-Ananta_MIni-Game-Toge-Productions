using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class TimelineManager : MonoBehaviour
{
    [Serializable]
    private class CutsceneDefinition
    {
        public string id;
        public PlayableDirector director;
        public bool playOnlyOnce = true;
    }

    [SerializeField] private List<CutsceneDefinition> cutscenes = new();

    [Header("Scene Start")]
    [SerializeField] private string autoPlayCutsceneId;

    private static readonly HashSet<string> completedCutscenes = new();
    private PlayableDirector activeDirector;

    public static TimelineManager Instance { get; private set; }
    public bool IsCutscenePlaying { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void BeginNewSession()
    {
        completedCutscenes.Clear();
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;

        foreach (CutsceneDefinition cutscene in cutscenes)
        {
            if (cutscene.director == null)
                continue;

            cutscene.director.playOnAwake = false;
            cutscene.director.stopped += HandleCutsceneStopped;
        }
    }

    private void Start()
    {
        if (!string.IsNullOrWhiteSpace(autoPlayCutsceneId))
            PlayCutscene(autoPlayCutsceneId);
    }

    private void OnDestroy()
    {
        foreach (CutsceneDefinition cutscene in cutscenes)
        {
            if (cutscene.director != null)
                cutscene.director.stopped -= HandleCutsceneStopped;
        }

        if (Instance == this)
            Instance = null;
    }

    public void PlayCutscene(string cutsceneId)
    {
        CutsceneDefinition cutscene = FindCutscene(cutsceneId);

        if (cutscene == null || cutscene.director == null)
            return;

        if (cutscene.playOnlyOnce && HasCompleted(cutsceneId))
            return;

        if (IsCutscenePlaying)
        {
            Debug.LogWarning($"Cannot play cutscene '{cutsceneId}' while another cutscene is playing.");
            return;
        }

        IsCutscenePlaying = true;
        activeDirector = cutscene.director;
        cutscene.director.time = 0;
        cutscene.director.Play();
    }

    public bool PauseCurrentCutscene()
    {
        if (activeDirector == null || !IsCutscenePlaying)
            return false;

        activeDirector.Pause();
        return true;
    }

    public void ResumeCurrentCutscene()
    {
        if (activeDirector != null && IsCutscenePlaying)
            activeDirector.Resume();
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Cannot load a scene without a scene name.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"Scene '{sceneName}' could not be loaded. Add it to the Build Profile first."
            );
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void ReplayCutscene(string cutsceneId)
    {
        completedCutscenes.Remove(cutsceneId);
        PlayCutscene(cutsceneId);
    }

    public static bool HasCompleted(string cutsceneId)
    {
        return !string.IsNullOrWhiteSpace(cutsceneId) &&
               completedCutscenes.Contains(cutsceneId);
    }

    public static void ResetAllCutsceneProgress()
    {
        completedCutscenes.Clear();
    }

    private CutsceneDefinition FindCutscene(string cutsceneId)
    {
        CutsceneDefinition cutscene = cutscenes.Find(entry => entry.id == cutsceneId);

        if (cutscene == null)
            Debug.LogWarning($"TimelineManager has no cutscene registered with ID '{cutsceneId}'.");

        return cutscene;
    }

    private void HandleCutsceneStopped(PlayableDirector stoppedDirector)
    {
        CutsceneDefinition cutscene =
            cutscenes.Find(entry => entry.director == stoppedDirector);

        if (cutscene != null &&
            cutscene.playOnlyOnce &&
            stoppedDirector.time >= stoppedDirector.duration - 0.05d)
        {
            completedCutscenes.Add(cutscene.id);
        }

        activeDirector = null;
        IsCutscenePlaying = false;
    }
}
