using System;
using System.Collections;
using System.Collections.Generic;
using Fungus;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class QuestManager : MonoBehaviour
{
    public enum QuestState
    {
        NotStarted,
        Ongoing,
        Finished
    }

    private sealed class TargetBinding
    {
        public GameObject target;
        public Interactable interactable;
        public DialogueTrigger dialogueTrigger;
        public List<QuestSO> quests;
        public QuestSO activeQuest;
        public Action<GameObject> interactionHandler;
        public Func<DialogueTrigger.DialogueStage> stageResolver;
        public bool managerAddedDialogueTrigger;
    }

    private const string PlayerPrefsPrefix = "QuestManager.";
    private const string GameplaySceneName = "Gameplay";

    [Header("Quests")]
    [SerializeField] private List<QuestSO> quests = new();

    [Header("Quest Tracker UI")]
    [Tooltip("Optional. If these references are empty, a tracker UI is created automatically.")]
    [SerializeField] private GameObject questTrackerPanel;
    [SerializeField] private TextMeshProUGUI questTitleText;
    [SerializeField] private TextMeshProUGUI questDescriptionText;

    private readonly List<TargetBinding> targetBindings = new();
    private Inventory subscribedInventory;
    private QuestSO displayedCompletionQuest;

    public static QuestManager Instance { get; private set; }
    public IReadOnlyList<QuestSO> Quests => quests;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureQuestTrackerUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TimelineManager.CutsceneFinished += HandleCutsceneFinished;
        BlockSignals.OnBlockEnd += HandleFlowchartBlockFinished;
        BattleRelay.UnitDefeated += HandleUnitDefeated;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        TimelineManager.CutsceneFinished -= HandleCutsceneFinished;
        BlockSignals.OnBlockEnd -= HandleFlowchartBlockFinished;
        BattleRelay.UnitDefeated -= HandleUnitDefeated;
        UnsubscribeInventoryEvents();
    }

    private void Start()
    {
        StartCoroutine(RebindAfterSceneLoad());
        SubscribeInventoryEvents();
        RefreshQuestTracker();
    }

    public QuestState GetState(string questId)
    {
        QuestSO quest = FindQuest(questId);
        return quest == null
            ? QuestState.NotStarted
            : GetState(quest);
    }

    public int GetProgress(string questId)
    {
        QuestSO quest = FindQuest(questId);
        return quest == null ? 0 : GetProgress(quest);
    }

    public int GetRequiredAmount(string questId)
    {
        QuestSO quest = FindQuest(questId);
        return quest == null ? 0 : GetRequirementAmount(quest);
    }

    public void ResetQuest(string questId)
    {
        QuestSO quest = FindQuest(questId);
        if (quest == null)
            return;

        SaveDataTransaction.DeleteKey(GetStateKey(quest));
        SaveDataTransaction.DeleteKey(GetKillBaselineKey(quest));
        SaveDataTransaction.Save();
        RefreshQuestTracker();
    }

    public void StartQuest(string questId)
    {
        QuestSO quest = FindQuest(questId);
        if (quest == null || GetState(quest) != QuestState.NotStarted)
            return;

        InitializeRequirementProgress(quest);
        SetState(quest, QuestState.Ongoing);
        RefreshQuestTracker();
    }

    [ContextMenu("Rebind Quest Targets")]
    public void BindQuestTargets()
    {
        ClearBindings();

        Dictionary<GameObject, List<QuestSO>> questsByTarget = new();

        foreach (QuestSO quest in quests)
        {
            if (!ValidateQuest(quest))
                continue;

            // Quests with no target are standalone - they're tracked
            // purely through StartQuest/GetState calls from elsewhere
            // (dialogue, triggers, code), no NPC binding needed.
            if (!quest.HasTarget)
                continue;

            GameObject runtimeTarget = quest.ResolveRuntimeTarget();
            if (runtimeTarget == null)
                continue;

            if (!questsByTarget.TryGetValue(
                    runtimeTarget,
                    out List<QuestSO> targetQuests))
            {
                targetQuests = new List<QuestSO>();
                questsByTarget.Add(runtimeTarget, targetQuests);
            }

            targetQuests.Add(quest);
        }

        foreach (KeyValuePair<GameObject, List<QuestSO>> pair
                 in questsByTarget)
        {
            BindTarget(pair.Key, pair.Value);
        }
    }

    private void BindTarget(
        GameObject target,
        List<QuestSO> targetQuests)
    {
        DialogueTrigger dialogueTrigger =
            target.GetComponent<DialogueTrigger>();
        bool managerAddedDialogueTrigger = dialogueTrigger == null;

        if (managerAddedDialogueTrigger)
            dialogueTrigger = target.AddComponent<DialogueTrigger>();

        Interactable interactable =
            target.GetComponent<Interactable>() ??
            target.AddComponent<Interactable>();

        TargetBinding binding = new()
        {
            target = target,
            interactable = interactable,
            dialogueTrigger = dialogueTrigger,
            quests = targetQuests,
            managerAddedDialogueTrigger = managerAddedDialogueTrigger
        };

        binding.stageResolver =
            () => AdvanceQuestFromInteraction(binding.activeQuest);
        binding.interactionHandler = _ =>
        {
            binding.dialogueTrigger.TriggerDialogue();

            if (binding.activeQuest != null &&
                GetState(binding.activeQuest) == QuestState.Finished)
            {
                ConfigureNextQuest(binding);
            }
        };

        interactable.Interacted += binding.interactionHandler;
        targetBindings.Add(binding);
        ConfigureNextQuest(binding);
    }

    private void ConfigureNextQuest(TargetBinding binding)
    {
        binding.dialogueTrigger.ClearQuestDialogue();
        binding.activeQuest = SelectQuest(binding.quests);

        if (binding.activeQuest == null)
        {
            RemoveBinding(binding);
            return;
        }

        List<KeyValuePair<DialogueTrigger.DialogueStage, string>>
            generatedOptions = new();

        foreach (QuestSO.DialogueOption option
                 in binding.activeQuest.dialogueOptions)
        {
            if (option != null)
            {
                generatedOptions.Add(
                    new KeyValuePair<DialogueTrigger.DialogueStage, string>(
                        option.stage,
                        option.flowchartBlockId
                    )
                );
            }
        }

        binding.dialogueTrigger.ConfigureQuestDialogue(
            binding.stageResolver,
            generatedOptions
        );
    }

    private QuestSO SelectQuest(List<QuestSO> targetQuests)
    {
        if (targetQuests == null || targetQuests.Count == 0)
            return null;

        foreach (QuestSO quest in targetQuests)
        {
            if (GetState(quest) != QuestState.Finished)
                return quest;
        }

        return null;
    }

    private DialogueTrigger.DialogueStage AdvanceQuestFromInteraction(
        QuestSO quest)
    {
        if (quest == null)
            return DialogueTrigger.DialogueStage.Ongoing;

        QuestState state = GetState(quest);

        if (state == QuestState.NotStarted)
        {
            StartQuest(quest.questId);
            return DialogueTrigger.DialogueStage.Start;
        }

        if (state == QuestState.Ongoing)
        {
            if (TryFinishRequirement(quest))
            {
                SetState(quest, QuestState.Finished);
                RefreshQuestTracker();
                return DialogueTrigger.DialogueStage.Finished;
            }

            return DialogueTrigger.DialogueStage.Ongoing;
        }

        return DialogueTrigger.DialogueStage.Finished;
    }

    private bool TryFinishRequirement(QuestSO quest)
    {
        int requiredAmount = GetRequirementAmount(quest);

        switch (quest.requirementType)
        {
            case QuestSO.RequirementType.GetItem:
                return Inventory.Instance != null &&
                       Inventory.Instance.TryRemoveItem(
                           quest.requiredItem,
                           requiredAmount
                       );

            case QuestSO.RequirementType.KillMonster:
                return GetProgress(quest) >= requiredAmount;

            default:
                return false;
        }
    }

    private int GetProgress(QuestSO quest)
    {
        int requiredAmount = GetRequirementAmount(quest);

        switch (quest.requirementType)
        {
            case QuestSO.RequirementType.GetItem:
                return Inventory.Instance == null
                    ? 0
                    : Mathf.Min(
                        requiredAmount,
                        Inventory.Instance.GetItemAmount(
                            quest.requiredItem
                        )
                    );

            case QuestSO.RequirementType.KillMonster:
                int baseline = SaveDataTransaction.GetInt(
                    GetKillBaselineKey(quest),
                    BattleRelay.GetDefeatedUnitCount(
                        quest.requiredMonster
                    )
                );
                return Mathf.Clamp(
                    BattleRelay.GetDefeatedUnitCount(
                        quest.requiredMonster
                    ) - baseline,
                    0,
                    requiredAmount
                );

            default:
                return 0;
        }
    }

    private static int GetRequirementAmount(QuestSO quest)
    {
        return quest.requirementType == QuestSO.RequirementType.GetItem
            ? Mathf.Max(1, quest.requiredItemAmount)
            : Mathf.Max(1, quest.requiredMonsterAmount);
    }

    private static void InitializeRequirementProgress(QuestSO quest)
    {
        if (quest.requirementType != QuestSO.RequirementType.KillMonster)
            return;

        SaveDataTransaction.SetInt(
            GetKillBaselineKey(quest),
            BattleRelay.GetDefeatedUnitCount(
                quest.requiredMonster
            )
        );
        SaveDataTransaction.Save();
    }

    private void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode loadSceneMode)
    {
        SubscribeInventoryEvents();
        RefreshQuestTracker();
        StartCoroutine(RebindAfterSceneLoad());

        if (scene.name == GameplaySceneName)
            StartCoroutine(ResolveKillQuestsAfterGameplayLoad());
    }

    private void HandleCutsceneFinished(string cutsceneId)
    {
        foreach (QuestSO quest in quests)
        {
            if (quest != null &&
                !string.IsNullOrWhiteSpace(
                    quest.startAfterCutsceneId
                ) &&
                quest.startAfterCutsceneId == cutsceneId)
            {
                StartQuest(quest.questId);
            }
        }
    }

    private void HandleFlowchartBlockFinished(Block block)
    {
        if (block == null ||
            string.IsNullOrWhiteSpace(block.BlockName))
            return;

        foreach (QuestSO quest in quests)
        {
            if (quest != null &&
                !string.IsNullOrWhiteSpace(
                    quest.startAfterFlowchartBlockName
                ) &&
                string.Equals(
                    quest.startAfterFlowchartBlockName,
                    block.BlockName,
                    StringComparison.Ordinal
                ))
            {
                StartQuest(quest.questId);
            }
        }
    }

    private void HandleUnitDefeated(UnitData unitData)
    {
        RefreshQuestTracker();
    }

    private IEnumerator RebindAfterSceneLoad()
    {
        // InteriorSpawner creates the selected house during Start.
        // Waiting two frames lets the spawned NPC finish Awake/Start first.
        yield return null;
        yield return null;
        SubscribeInventoryEvents();
        BindQuestTargets();
        RefreshQuestTracker();
    }

    private IEnumerator ResolveKillQuestsAfterGameplayLoad()
    {
        // Let the Gameplay flowchart initialize before requesting dialogue.
        yield return null;
        yield return null;

        foreach (QuestSO quest in quests)
        {
            if (quest == null ||
                quest.HasTarget ||
                quest.requirementType != QuestSO.RequirementType.KillMonster ||
                GetState(quest) != QuestState.Ongoing ||
                GetProgress(quest) < GetRequirementAmount(quest))
            {
                continue;
            }

            SetState(quest, QuestState.Finished);
            displayedCompletionQuest = quest;
            RefreshQuestTracker();

            string finishedBlock = GetDialogueBlock(
                quest,
                DialogueTrigger.DialogueStage.Finished);

            if (string.IsNullOrWhiteSpace(finishedBlock))
            {
                displayedCompletionQuest = null;
                RefreshQuestTracker();
                continue;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning(
                    $"Quest '{quest.questId}' finished, but its dialogue could not play because DialogueManager is unavailable.");
                displayedCompletionQuest = null;
                RefreshQuestTracker();
                continue;
            }

            yield return DialogueManager.Instance.PlayDialogueAndWait(
                finishedBlock);

            displayedCompletionQuest = null;
            RefreshQuestTracker();
        }
    }

    private static string GetDialogueBlock(
        QuestSO quest,
        DialogueTrigger.DialogueStage stage)
    {
        if (quest == null || quest.dialogueOptions == null)
            return null;

        foreach (QuestSO.DialogueOption option in quest.dialogueOptions)
        {
            if (option != null &&
                option.stage == stage &&
                !string.IsNullOrWhiteSpace(option.flowchartBlockId))
            {
                return option.flowchartBlockId;
            }
        }

        return null;
    }

    private void SubscribeInventoryEvents()
    {
        if (subscribedInventory == Inventory.Instance)
            return;

        UnsubscribeInventoryEvents();
        subscribedInventory = Inventory.Instance;

        if (subscribedInventory != null)
            subscribedInventory.InventoryChanged += RefreshQuestTracker;
    }

    private void UnsubscribeInventoryEvents()
    {
        if (subscribedInventory != null)
            subscribedInventory.InventoryChanged -= RefreshQuestTracker;

        subscribedInventory = null;
    }

    private void RefreshQuestTracker()
    {
        EnsureQuestTrackerUI();

        QuestSO trackedQuest =
            displayedCompletionQuest != null
                ? displayedCompletionQuest
                : FindTrackedQuest();

        bool shouldShow =
            trackedQuest != null &&
            IsQuestTrackerAllowedInCurrentScene();

        if (questTrackerPanel != null)
            questTrackerPanel.SetActive(shouldShow);

        if (!shouldShow)
            return;

        questTitleText.text =
            !string.IsNullOrWhiteSpace(trackedQuest.displayName)
                ? trackedQuest.displayName
                : trackedQuest.questId;

        questDescriptionText.text =
            BuildObjectiveDescription(trackedQuest);
    }

    private QuestSO FindTrackedQuest()
    {
        foreach (QuestSO quest in quests)
        {
            if (quest != null && GetState(quest) == QuestState.Ongoing)
                return quest;
        }

        return null;
    }

    private string BuildObjectiveDescription(QuestSO quest)
    {
        int requiredAmount = GetRequirementAmount(quest);
        int progress = Mathf.Clamp(
            GetProgress(quest),
            0,
            requiredAmount);
        string progressText = $"({progress}/{requiredAmount})";

        if (!string.IsNullOrWhiteSpace(quest.description))
            return $"{quest.description.Trim()} {progressText}";

        switch (quest.requirementType)
        {
            case QuestSO.RequirementType.GetItem:
                {
                    string itemName =
                        quest.requiredItem != null &&
                        !string.IsNullOrWhiteSpace(
                            quest.requiredItem.ItemName)
                            ? quest.requiredItem.ItemName
                            : "item";

                    return $"Get {requiredAmount} {itemName} {progressText}";
                }

            case QuestSO.RequirementType.KillMonster:
                {
                    string monsterName =
                        quest.requiredMonster != null &&
                        !string.IsNullOrWhiteSpace(
                            quest.requiredMonster.unitName)
                            ? quest.requiredMonster.unitName
                            : "enemy";

                    return $"Kill {requiredAmount} {monsterName} {progressText}";
                }

            default:
                return string.Empty;
        }
    }

    private static bool IsQuestTrackerAllowedInCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName != "Battle" &&
               sceneName != "Main Menu" &&
               sceneName != "Initializer";
    }

    private void EnsureQuestTrackerUI()
    {
        if (questTrackerPanel != null &&
            questTitleText != null &&
            questDescriptionText != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            "Quest Tracker Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        questTrackerPanel = new GameObject(
            "Quest Tracker",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        questTrackerPanel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect =
            questTrackerPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(520f, 120f);

        Image background = questTrackerPanel.GetComponent<Image>();
        background.color = new Color(0.035f, 0.045f, 0.065f, 0.88f);
        background.raycastTarget = false;

        questTitleText = CreateTrackerText(
            "Quest Title",
            panelRect,
            24f,
            FontStyles.Bold,
            new Vector2(0f, -12f),
            new Vector2(-32f, 32f));

        questDescriptionText = CreateTrackerText(
            "Quest Description",
            panelRect,
            18f,
            FontStyles.Normal,
            new Vector2(0f, -52f),
            new Vector2(-32f, 52f));

        questTrackerPanel.SetActive(false);
    }

    private static TextMeshProUGUI CreateTrackerText(
        string objectName,
        RectTransform parent,
        float fontSize,
        FontStyles fontStyle,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = sizeDelta;

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;

        return text;
    }

    private static bool ValidateQuest(QuestSO quest)
    {
        if (quest == null || string.IsNullOrWhiteSpace(quest.questId))
        {
            Debug.LogWarning(
                "QuestManager contains a quest without an ID."
            );
            return false;
        }

        return quest.requirementType == QuestSO.RequirementType.GetItem
            ? quest.requiredItem != null
            : quest.requiredMonster != null;
    }

    private QuestSO FindQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return null;

        return quests.Find(
            quest => quest != null &&
                     string.Equals(
                         quest.questId,
                         questId,
                         StringComparison.Ordinal
                     )
        );
    }

    private static QuestState GetState(QuestSO quest)
    {
        int savedState = SaveDataTransaction.GetInt(
            GetStateKey(quest),
            (int)QuestState.NotStarted
        );

        return Enum.IsDefined(typeof(QuestState), savedState)
            ? (QuestState)savedState
            : QuestState.NotStarted;
    }

    private static void SetState(QuestSO quest, QuestState state)
    {
        SaveDataTransaction.SetInt(
            GetStateKey(quest),
            (int)state
        );
        SaveDataTransaction.Save();
    }

    private static string GetStateKey(QuestSO quest)
    {
        return PlayerPrefsPrefix + quest.questId.Trim() + ".State";
    }

    private static string GetKillBaselineKey(QuestSO quest)
    {
        return PlayerPrefsPrefix +
               quest.questId.Trim() +
               ".KillBaseline";
    }

    private void ClearBindings()
    {
        for (int i = targetBindings.Count - 1; i >= 0; i--)
        {
            RemoveBinding(targetBindings[i]);
        }
    }

    private void RemoveBinding(TargetBinding binding)
    {
        if (binding == null)
            return;

        if (binding.interactable != null)
            binding.interactable.Interacted -= binding.interactionHandler;

        if (binding.dialogueTrigger != null)
        {
            binding.dialogueTrigger.ClearQuestDialogue();

            if (binding.managerAddedDialogueTrigger)
                Destroy(binding.dialogueTrigger);
        }

        targetBindings.Remove(binding);
    }

    private void OnDestroy()
    {
        ClearBindings();

        if (Instance == this)
            Instance = null;
    }
}
