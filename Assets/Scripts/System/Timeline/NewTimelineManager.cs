using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class NewTimelineManager : MonoBehaviour
{
    private const string PlayerPrefsPrefix = "NewTimelineManager.HasPlayed.";

    public static NewTimelineManager Instance { get; private set; }
    public static bool IsAnyCutscenePlaying =>
        Instance != null && Instance.currentPlayableDirector != null;

    public static event Action<string> CutsceneFinished;

    [SerializeField] private List<TimelineEntry> timelineEntries = new();

    [Header("Scene Start")]
    [SerializeField] private string playOnStartId;

    private TimelineEntry currentTimelineEntry;
    private PlayableDirector currentPlayableDirector;
    private int pauseRequestCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        CutsceneFinished = null;
    }

    private void Awake()
    {
        Instance = this;

        foreach (TimelineEntry entry in timelineEntries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                continue;

            entry.id = entry.id.Trim();
            entry.hasPlayed = LoadHasPlayed(entry.id);

            if (entry.playableDirector != null)
            {
                entry.playableDirector.playOnAwake = false;
                entry.playableDirector.gameObject.SetActive(
                    !entry.hasPlayed
                );
            }
        }
    }

    private void Start()
    {
        if (!string.IsNullOrWhiteSpace(playOnStartId))
            PlayTimeline(playOnStartId);
    }

    private void OnDestroy()
    {
        UnsubscribeFromCurrentDirector();

        if (Instance == this)
            Instance = null;
    }

    public bool PlayTimeline(string id)
    {
        TimelineEntry entry = FindTimeline(id);

        if (entry == null || entry.hasPlayed)
            return false;

        if (entry.playableDirector == null)
        {
            Debug.LogWarning(
                $"Timeline '{entry.id}' has no PlayableDirector assigned.",
                this
            );
            return false;
        }

        if (currentPlayableDirector != null)
        {
            Debug.LogWarning(
                "Another timeline is already active.",
                this
            );
            return false;
        }

        currentTimelineEntry = entry;
        currentPlayableDirector = entry.playableDirector;
        pauseRequestCount = 0;
        currentPlayableDirector.gameObject.SetActive(true);
        currentPlayableDirector.stopped += HandleTimelineStopped;
        currentPlayableDirector.time = 0d;
        currentPlayableDirector.Play();
        return true;
    }

    public bool PauseTimeline()
    {
        if (currentPlayableDirector == null)
            return false;

        pauseRequestCount++;

        if (pauseRequestCount == 1 &&
            currentPlayableDirector.state == PlayState.Playing)
        {
            currentPlayableDirector.Pause();
        }

        return true;
    }

    public bool ResumeTimeline()
    {
        if (currentPlayableDirector == null || pauseRequestCount <= 0)
            return false;

        pauseRequestCount--;

        if (pauseRequestCount == 0 &&
            currentPlayableDirector.state == PlayState.Paused)
        {
            currentPlayableDirector.Resume();
        }

        return true;
    }

    public bool CompleteTimeline()
    {
        if (currentPlayableDirector == null ||
            currentTimelineEntry == null)
        {
            return false;
        }

        if (currentPlayableDirector.duration > 0d &&
            currentPlayableDirector.time <
            currentPlayableDirector.duration - 0.0001d)
        {
            currentPlayableDirector.time =
                currentPlayableDirector.duration;
            currentPlayableDirector.Evaluate();
        }

        MarkAsPlayed(currentTimelineEntry);
        ClearCurrentTimeline(true);
        return true;
    }

    public bool HasPlayed(string id)
    {
        TimelineEntry entry = FindTimeline(id, false);
        return entry != null && entry.hasPlayed;
    }

    public void ResetTimelineProgress(string id)
    {
        TimelineEntry entry = FindTimeline(id);
        if (entry == null)
            return;

        entry.hasPlayed = false;
        PlayerPrefs.DeleteKey(GetPlayerPrefsKey(entry.id));
        PlayerPrefs.Save();

        if (entry.playableDirector != null)
            entry.playableDirector.gameObject.SetActive(true);
    }

    private void HandleTimelineStopped(PlayableDirector director)
    {
        if (director != currentPlayableDirector)
            return;

        // With Wrap Mode set to None, Unity may reset the director's
        // time before invoking stopped. Reaching this subscribed callback
        // means the active Timeline ended naturally. Explicit completion
        // unsubscribes before calling Stop(), so it cannot double-complete.
        if (currentTimelineEntry != null)
            MarkAsPlayed(currentTimelineEntry);

        ClearCurrentTimeline(false);
    }

    private void MarkAsPlayed(TimelineEntry entry)
    {
        bool wasAlreadyPlayed = entry.hasPlayed;
        entry.hasPlayed = true;
        PlayerPrefs.SetInt(GetPlayerPrefsKey(entry.id), 1);
        PlayerPrefs.Save();

        if (!wasAlreadyPlayed)
            CutsceneFinished?.Invoke(entry.id);
    }

    private void ClearCurrentTimeline(bool stopDirector)
    {
        PlayableDirector director = currentPlayableDirector;
        bool shouldDeactivate =
            currentTimelineEntry != null &&
            currentTimelineEntry.hasPlayed;

        UnsubscribeFromCurrentDirector();

        currentPlayableDirector = null;
        currentTimelineEntry = null;
        pauseRequestCount = 0;

        if (stopDirector && director != null)
            director.Stop();

        if (director != null && shouldDeactivate)
            director.gameObject.SetActive(false);
    }

    private void UnsubscribeFromCurrentDirector()
    {
        if (currentPlayableDirector != null)
            currentPlayableDirector.stopped -= HandleTimelineStopped;
    }

    private TimelineEntry FindTimeline(
        string id,
        bool logWarning = true)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        string trimmedId = id.Trim();
        TimelineEntry entry = timelineEntries.Find(
            timeline => timeline != null &&
                        string.Equals(
                            timeline.id,
                            trimmedId,
                            StringComparison.Ordinal
                        )
        );

        if (entry == null && logWarning)
        {
            Debug.LogWarning(
                $"No timeline with ID '{trimmedId}' was found.",
                this
            );
        }

        return entry;
    }

    private static bool LoadHasPlayed(string id)
    {
        return PlayerPrefs.GetInt(GetPlayerPrefsKey(id), 0) == 1;
    }

    private static string GetPlayerPrefsKey(string id)
    {
        return PlayerPrefsPrefix + id.Trim();
    }
}

[Serializable]
public class TimelineEntry
{
    public string id;
    public PlayableDirector playableDirector;

    [NonSerialized] public bool hasPlayed;
}

