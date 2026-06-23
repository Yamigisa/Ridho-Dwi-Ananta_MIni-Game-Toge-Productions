using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class DialogueTrigger : MonoBehaviour
{
    public enum DialogueStage
    {
        Start,
        Ongoing,
        Finished
    }

    [Header("Default Dialogue")]
    [SerializeField] private string dialogueBlockName;

    private PlayableDirector playableDirector;
    private Coroutine dialogueTimelineRoutine;
    private readonly Dictionary<DialogueStage, string>
        runtimeDialogueOptions = new();
    private Func<DialogueStage> runtimeStageResolver;
    private int lastTriggerFrame = -1;

    private void Awake()
    {
        playableDirector = GetComponent<PlayableDirector>();
    }

    public void TriggerDialogue()
    {
        // Prevent two interaction callbacks in the same frame from
        // launching the same dialogue twice.
        if (lastTriggerFrame == Time.frameCount)
            return;

        lastTriggerFrame = Time.frameCount;

        string selectedBlock = null;

        if (runtimeStageResolver != null)
        {
            DialogueStage stage = runtimeStageResolver();
            runtimeDialogueOptions.TryGetValue(
                stage,
                out selectedBlock
            );
        }

        if (string.IsNullOrWhiteSpace(selectedBlock))
            selectedBlock = dialogueBlockName;

        TriggerDialogue(selectedBlock);
    }

    public void TriggerDialogue(string blockName)
    {
        if (!string.IsNullOrWhiteSpace(blockName) &&
            DialogueManager.Instance != null)
        {
            DialogueManager.Instance.PlayDialogue(blockName);
        }
    }

    public void ConfigureQuestDialogue(
        Func<DialogueStage> stageResolver,
        IEnumerable<KeyValuePair<DialogueStage, string>> options)
    {
        runtimeStageResolver = stageResolver;
        runtimeDialogueOptions.Clear();

        if (options == null)
            return;

        foreach (KeyValuePair<DialogueStage, string> option in options)
        {
            if (!string.IsNullOrWhiteSpace(option.Value))
            {
                runtimeDialogueOptions[option.Key] = option.Value;
            }
        }
    }

    public void ClearQuestDialogue()
    {
        runtimeStageResolver = null;
        runtimeDialogueOptions.Clear();
    }

    public void TriggerDialogueAndPauseTimeline(string blockName)
    {
        if (dialogueTimelineRoutine != null)
            return;

        dialogueTimelineRoutine =
            StartCoroutine(PlayDialogueAndResumeTimeline(blockName));
    }

    private IEnumerator PlayDialogueAndResumeTimeline(string blockName)
    {
        if (playableDirector == null)
        {
            Debug.LogWarning(
                "DialogueTrigger requires a PlayableDirector on the same GameObject."
            );
            dialogueTimelineRoutine = null;
            yield break;
        }

        bool pausedThroughManager =
            NewTimelineManager.Instance != null &&
            NewTimelineManager.Instance.PauseTimeline();

        if (!pausedThroughManager)
            playableDirector.Pause();

        try
        {
            yield return DialogueManager.Instance.PlayDialogueAndWait(blockName);
        }
        finally
        {
            if (pausedThroughManager &&
                NewTimelineManager.Instance != null)
            {
                NewTimelineManager.Instance.ResumeTimeline();
            }
            else if (playableDirector != null)
            {
                playableDirector.Resume();
            }

            dialogueTimelineRoutine = null;
        }
    }
}
