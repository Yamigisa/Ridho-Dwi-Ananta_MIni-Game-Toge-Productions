using UnityEngine;
using Fungus;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

[DisallowMultipleComponent]
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
    public static bool IsGameplayInputLocked =>
        (Instance != null && Instance.IsDialoguePlaying) ||
        TimelineManager.IsAnyCutscenePlaying;
    public bool IsDialoguePlaying =>
        activeDialogueRequests > 0 ||
        executingBlocks.Count > 0 ||
        IsDialogueUiBusy(flowchart);

    public event Action<bool> OnPopupVisibilityChanged;
    private bool isPopupVisible;
    private int popupSequenceDepth;
    private int activeDialogueRequests;
    private float dialogueIdleTime;
    private readonly HashSet<Block> executingBlocks = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

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
        BlockSignals.OnBlockStart += HandleBlockStarted;
        BlockSignals.OnBlockEnd += HandleBlockEnded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        BlockSignals.OnBlockStart -= HandleBlockStarted;
        BlockSignals.OnBlockEnd -= HandleBlockEnded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        flowchart = null;
        activeDialogueRequests = 0;
        dialogueIdleTime = 0f;
        executingBlocks.Clear();
    }

    private void HandleBlockStarted(Block block)
    {
        if (block != null)
            executingBlocks.Add(block);
    }

    private void HandleBlockEnded(Block block)
    {
        if (block != null)
            executingBlocks.Remove(block);
    }

    private void Update()
    {
        if (activeDialogueRequests <= 0)
        {
            dialogueIdleTime = 0f;
            return;
        }

        if (IsDialogueUiBusy(flowchart))
        {
            dialogueIdleTime = 0f;
            return;
        }

        dialogueIdleTime += Time.unscaledDeltaTime;

        // If a third-party dialogue coroutine throws, Unity can terminate it
        // before our request counter unwinds. Release the stale gameplay lock
        // once Fungus and its menu have both remained idle.
        if (dialogueIdleTime >= 0.5f)
        {
            activeDialogueRequests = 0;
            dialogueIdleTime = 0f;
        }
    }

    public void PlayDialogue(string blockName)
    {
        StartCoroutine(PlayDialogueWhenReady(blockName));
    }

    public IEnumerator PlayDialogueAndWait(string blockName)
    {
        yield return PlayDialogueWhenReady(blockName);
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

        float idleTime = 0f;

        // Fungus briefly has no executing block when a menu option hands off
        // to its target block. Require a short continuously-idle window so
        // that gap cannot release gameplay input.
        while (idleTime < 0.1f)
        {
            if (IsDialogueUiBusy(targetFlowchart))
                idleTime = 0f;
            else
                idleTime += Time.unscaledDeltaTime;

            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);
    }

    private static bool IsDialogueUiBusy(Flowchart targetFlowchart)
    {
        if (targetFlowchart != null && targetFlowchart.HasExecutingBlocks())
            return true;

        MenuDialog menuDialog = MenuDialog.ActiveMenuDialog;
        return menuDialog != null &&
               menuDialog.IsActive() &&
               menuDialog.DisplayedOptionsCount > 0;
    }

    private IEnumerator PlayDialogueWhenReady(string blockName)
    {
        activeDialogueRequests++;

        try
        {
            yield return RunDialogueWhenReady(blockName);
        }
        finally
        {
            activeDialogueRequests = Mathf.Max(0, activeDialogueRequests - 1);
            dialogueIdleTime = 0f;
        }
    }

    private IEnumerator RunDialogueWhenReady(string blockName)
    {
        if (string.IsNullOrWhiteSpace(blockName))
            yield break;

        Flowchart targetFlowchart = null;

        for (int frame = 0; frame < flowchartInitializationFrames; frame++)
        {
            if (TryFindFlowchartWithBlock(blockName, out targetFlowchart))
                break;

            yield return null;
        }

        if (targetFlowchart == null)
            yield break;

        flowchart = targetFlowchart;
        targetFlowchart.ExecuteBlock(blockName);
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
            return false;

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
