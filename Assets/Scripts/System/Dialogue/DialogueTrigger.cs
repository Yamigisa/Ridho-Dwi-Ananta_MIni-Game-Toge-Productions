using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private string dialogueBlockName;

    private PlayableDirector playableDirector;
    private Coroutine dialogueTimelineRoutine;

    private void Awake()
    {
        playableDirector = GetComponent<PlayableDirector>();
    }

    public void TriggerDialogue()
    {
        DialogueManager.Instance.PlayDialogue(dialogueBlockName);
    }

    public void TriggerDialogue(string blockName)
    {
        DialogueManager.Instance.PlayDialogue(blockName);
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

        playableDirector.Pause();
        yield return DialogueManager.Instance.PlayDialogueAndWait(blockName);

        playableDirector.Resume();
        dialogueTimelineRoutine = null;
    }
}
