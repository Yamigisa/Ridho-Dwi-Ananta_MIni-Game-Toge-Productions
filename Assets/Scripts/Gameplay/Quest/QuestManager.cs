using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [SerializeField] private List<QuestSO> quests = new();

    private readonly List<TargetBinding> targetBindings = new();

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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        NewTimelineManager.CutsceneFinished += HandleCutsceneFinished;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        NewTimelineManager.CutsceneFinished -= HandleCutsceneFinished;
    }

    private void Start()
    {
        StartCoroutine(RebindAfterSceneLoad());
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
    }

    public void StartQuest(string questId)
    {
        QuestSO quest = FindQuest(questId);
        if (quest == null || GetState(quest) != QuestState.NotStarted)
            return;

        InitializeRequirementProgress(quest);
        SetState(quest, QuestState.Ongoing);
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
        StartCoroutine(RebindAfterSceneLoad());
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

    private IEnumerator RebindAfterSceneLoad()
    {
        // InteriorSpawner creates the selected house during Start.
        // Waiting two frames lets the spawned NPC finish Awake/Start first.
        yield return null;
        yield return null;
        BindQuestTargets();
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
