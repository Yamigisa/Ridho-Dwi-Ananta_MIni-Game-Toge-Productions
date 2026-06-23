using UnityEngine;
using Fungus;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("Flowchart")]
    [SerializeField] private Flowchart flowchart;

    [Header("Pop up Message")]
    [SerializeField] private PopupMessages popupMessages;
    public PopupMessages Messages => popupMessages;

    [SerializeField] private string popupBlockName = "PopupMessage";
    [SerializeField] private string popupVariableName = "PopupText";
    [SerializeField, Min(1)] private int flowchartInitializationFrames = 10;

    public static DialogueManager Instance { get; private set; }
    public bool IsDialoguePlaying => flowchart != null && flowchart.HasExecutingBlocks();

    public event Action<bool> OnPopupVisibilityChanged;
    private bool isPopupVisible;
    private int popupSequenceDepth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        flowchart = null;
    }

    public void PlayDialogue(string blockName)
    {
        StartCoroutine(PlayDialogueWhenReady(blockName, false));
    }

    public IEnumerator PlayDialogueAndWait(string blockName)
    {
        yield return PlayDialogueWhenReady(blockName, true);
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

        SetPopupVisible(true);
        popupFlowchart.SetStringVariable(popupVariableName, message);
        popupFlowchart.ExecuteBlock(popupBlockName);
        yield return WaitForDialogueToFinish(popupFlowchart);
        HidePopupIfNotSequencing();
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

    public IEnumerator ShowFormattedPopupForSeconds(float seconds, string template, params (string key, string value)[] replacements)
    {
        string message = template;
        foreach (var (key, value) in replacements)
            message = message.Replace($"{{{key}}}", value);

        yield return ShowPopupForSeconds(message, seconds);
    }

    public IEnumerator ShowPopupForSeconds(string message, float seconds)
    {
        yield return WaitUntilFlowchartFree();

        if (!TryFindPopupFlowchart(out Flowchart popupFlowchart))
            yield break;

        SetPopupVisible(true);
        popupFlowchart.SetStringVariable(popupVariableName, message);
        popupFlowchart.ExecuteBlock(popupBlockName);
        yield return new WaitForSeconds(seconds);
        yield return WaitForDialogueToFinish(popupFlowchart);
        HidePopupIfNotSequencing();
    }

    public void BeginPopupSequence()
    {
        popupSequenceDepth++;
        SetPopupVisible(true);
    }

    public void EndPopupSequence()
    {
        popupSequenceDepth = Mathf.Max(0, popupSequenceDepth - 1);
        HidePopupIfNotSequencing();
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

    private IEnumerator PlayDialogueWhenReady(string blockName, bool waitUntilFinished)
    {
        if (string.IsNullOrWhiteSpace(blockName))
        {
            Debug.LogWarning("Cannot play dialogue without a block name.");
            yield break;
        }

        Flowchart targetFlowchart = null;

        for (int frame = 0; frame < flowchartInitializationFrames; frame++)
        {
            if (TryFindFlowchartWithBlock(blockName, out targetFlowchart))
                break;

            yield return null;
        }

        if (targetFlowchart == null)
        {
            Debug.LogWarning($"Block {blockName} does not exist.");
            yield break;
        }

        flowchart = targetFlowchart;
        targetFlowchart.ExecuteBlock(blockName);

        if (waitUntilFinished)
            yield return WaitForDialogueToFinish(targetFlowchart);
    }

    private bool TryFindFlowchartWithBlock(
        string blockName,
        out Flowchart targetFlowchart)
    {
        Flowchart[] flowcharts =
            FindObjectsByType<Flowchart>(FindObjectsSortMode.None);

        foreach (Flowchart candidate in flowcharts)
        {
            if (candidate.HasBlock(blockName))
            {
                targetFlowchart = candidate;
                return true;
            }
        }

        targetFlowchart = null;
        return false;
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

    private void SetPopupVisible(bool visible)
    {
        if (isPopupVisible == visible)
            return;

        isPopupVisible = visible;
        OnPopupVisibilityChanged?.Invoke(visible);
    }

    private void HidePopupIfNotSequencing()
    {
        if (popupSequenceDepth > 0)
            return;

        SetPopupVisible(false);
    }
}
