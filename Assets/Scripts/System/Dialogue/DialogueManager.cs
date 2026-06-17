using UnityEngine;
using Fungus;
using UnityEngine.SceneManagement;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    [Header("Flowchart")]
    [SerializeField] private Flowchart flowchart;

    [Header("Pop up Message")]
    [SerializeField] private PopupMessages popupMessages;
    public PopupMessages Messages => popupMessages;

    [SerializeField] private string popupBlockName = "PopupMessage";
    [SerializeField] private string popupVariableName = "PopupText";

    public static DialogueManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        FindSceneFlowchart();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindSceneFlowchart();
    }

    public void PlayDialogue(string blockName)
    {
        if (!CanExecuteBlock(blockName))
            return;

        flowchart.ExecuteBlock(blockName);
    }

    public void ShowPopup(string message)
    {
        if (!TryFindPopupFlowchart(out Flowchart popupFlowchart))
            return;

        popupFlowchart.SetStringVariable(popupVariableName, message);
        popupFlowchart.ExecuteBlock(popupBlockName);
    }

    public IEnumerator ShowPopupAndWait(string message)
    {
        yield return WaitUntilFlowchartFree();

        if (!TryFindPopupFlowchart(out Flowchart popupFlowchart))
            yield break;

        popupFlowchart.SetStringVariable(popupVariableName, message);
        popupFlowchart.ExecuteBlock(popupBlockName);
        yield return WaitForDialogueToFinish(popupFlowchart);
    }

    public void ShowFormattedPopup(string template, params (string key, string value)[] replacements)
    {
        string message = template;
        foreach (var (key, value) in replacements)
            message = message.Replace($"{{{key}}}", value);

        ShowPopup(message);
    }

    public IEnumerator ShowFormattedPopupAndWait(string template, params (string key, string value)[] replacements)
    {
        string message = template;
        foreach (var (key, value) in replacements)
            message = message.Replace($"{{{key}}}", value);

        yield return ShowPopupAndWait(message);
    }

    public IEnumerator WaitUntilFlowchartFree()
    {
        TryFindPopupFlowchart(out Flowchart popupFlowchart);

        while (popupFlowchart != null && popupFlowchart.HasExecutingBlocks())
            yield return null;
    }

    public IEnumerator WaitForDialogueToFinish()
    {
        yield return WaitForDialogueToFinish(flowchart);
    }

    private IEnumerator WaitForDialogueToFinish(Flowchart targetFlowchart)
    {
        yield return null; // let the block actually start

        while (targetFlowchart != null && targetFlowchart.HasExecutingBlocks())
            yield return null;

        yield return new WaitForSeconds(0.3f);
    }

    private void FindSceneFlowchart()
    {
        Flowchart[] flowcharts = FindObjectsByType<Flowchart>(FindObjectsSortMode.None);
        flowchart = flowcharts.Length > 0 ? flowcharts[0] : null;
    }

    private bool CanShowPopup()
    {
        return TryFindPopupFlowchart(out _);
    }

    private bool CanExecuteBlock(string blockName)
    {
        FindSceneFlowchart();

        if (flowchart == null)
        {
            Debug.LogWarning($"No Flowchart found for block {blockName}.");
            return false;
        }

        if (!flowchart.HasBlock(blockName))
        {
            Debug.LogWarning($"Block {blockName} does not exist.");
            return false;
        }

        return true;
    }

    private bool TryFindPopupFlowchart(out Flowchart popupFlowchart)
    {
        Flowchart[] flowcharts = FindObjectsByType<Flowchart>(FindObjectsSortMode.None);

        foreach (Flowchart candidate in flowcharts)
        {
            if (candidate.HasVariable(popupVariableName) && candidate.HasBlock(popupBlockName))
            {
                flowchart = candidate;
                popupFlowchart = candidate;
                return true;
            }
        }

        popupFlowchart = null;

        if (flowcharts.Length == 0)
        {
            Debug.LogWarning("No Flowchart found for popup message.");
            return false;
        }

        foreach (Flowchart candidate in flowcharts)
        {
            if (!candidate.HasVariable(popupVariableName))
                Debug.LogWarning($"{candidate.name} is missing variable {popupVariableName}.");

            if (!candidate.HasBlock(popupBlockName))
                Debug.LogWarning($"{candidate.name} is missing block {popupBlockName}.");
        }

        return false;
    }
}
