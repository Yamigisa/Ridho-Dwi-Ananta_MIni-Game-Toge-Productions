using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private string dialogueBlockName;

    public void TriggerDialogue()
    {
        DialogueManager.Instance.PlayDialogue(dialogueBlockName);
    }
}
