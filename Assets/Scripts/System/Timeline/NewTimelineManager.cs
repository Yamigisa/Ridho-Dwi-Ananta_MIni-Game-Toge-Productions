using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class NewTimelineManager : MonoBehaviour
{
    // V2 ignores completion flags written by the previous quit-time bug.
    private const string PlayerPrefsPrefix = "NewTimelineManager.V2.HasPlayed.";

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
    private double lastObservedTimelineTime;
    private double currentTimelineDuration;
    private bool timelineReachedEnd;

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
        if (currentPlayableDirector != null)
            SaveDataTransaction.Rollback();

        UnsubscribeFromCurrentDirector();

        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationQuit()
    {
        if (currentPlayableDirector != null)
            SaveDataTransaction.Rollback();
    }

    private void Update()
    {
        if (currentPlayableDirector == null)
            return;

        lastObservedTimelineTime = currentPlayableDirector.time;

        double duration = currentTimelineDuration;
        if (duration <= 0d)
            return;

        double frameTolerance =
            Math.Max(0.001d, Time.unscaledDeltaTime * 2.5d);

        if (lastObservedTimelineTime >= duration - frameTolerance)
            timelineReachedEnd = true;
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
        lastObservedTimelineTime = 0d;
        timelineReachedEnd = false;
        SaveDataTransaction.Begin();
        currentPlayableDirector.gameObject.SetActive(true);
        currentPlayableDirector.stopped += HandleTimelineStopped;
        currentPlayableDirector.time = 0d;
        currentTimelineDuration =
            Math.Max(0d, currentPlayableDirector.duration);
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

        timelineReachedEnd = true;

        if (currentPlayableDirector.duration > 0d &&
            currentPlayableDirector.time <
            currentPlayableDirector.duration - 0.0001d)
        {
            currentPlayableDirector.time =
                currentPlayableDirector.duration;
            currentPlayableDirector.Evaluate();
        }

        MarkAsPlayed(currentTimelineEntry);
        SaveDataTransaction.Commit();
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
        SaveDataTransaction.DeleteKey(
            GetPlayerPrefsKey(entry.id));
        SaveDataTransaction.Save();

        if (entry.playableDirector != null)
            entry.playableDirector.gameObject.SetActive(true);
    }

    private void HandleTimelineStopped(PlayableDirector director)
    {
        if (director != currentPlayableDirector)
            return;

        bool completedNaturally =
            currentTimelineEntry != null &&
            DidTimelineReachEnd(director);

        if (completedNaturally)
        {
            MarkAsPlayed(currentTimelineEntry);
            SaveDataTransaction.Commit();
        }
        else
        {
            SaveDataTransaction.Rollback();
        }

        ClearCurrentTimeline(false);
    }

    private void MarkAsPlayed(TimelineEntry entry)
    {
        bool wasAlreadyPlayed = entry.hasPlayed;
        entry.hasPlayed = true;
        SaveDataTransaction.SetInt(
            GetPlayerPrefsKey(entry.id),
            1);

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
        lastObservedTimelineTime = 0d;
        currentTimelineDuration = 0d;
        timelineReachedEnd = false;

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

    private bool DidTimelineReachEnd(PlayableDirector director)
    {
        if (timelineReachedEnd)
            return true;

        double duration = currentTimelineDuration;
        if (duration <= 0d)
            return false;

        double frameTolerance =
            Math.Max(0.001d, Time.unscaledDeltaTime * 2.5d);

        return director.time >= duration - frameTolerance ||
               lastObservedTimelineTime >=
               duration - frameTolerance;
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
        return SaveDataTransaction.GetInt(
            GetPlayerPrefsKey(id),
            0) == 1;
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

