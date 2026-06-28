using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitSaveData : MonoBehaviour
{
    [Serializable]
    private class SavedData
    {
        public bool existsInWorld = true;
        public string sceneName;
        public string locationId;
        public Vector3 position;
        public int level;
        public int experience;
        public int currentHP;
        public int currentMP;
        public int attack;
        public int defense;
        public int speed;
        public List<string> addedSkillNames = new();
        public bool hasPartyData;
        public List<string> partyUnitIds = new();
    }

    [Serializable]
    private class SavedIdList
    {
        public List<string> ids = new();
    }

    private const string PlayerPrefsPrefix = "UnitSaveData.";
    private const string DefeatedEncountersKey =
        PlayerPrefsPrefix + "DefeatedEncounters";
    private static readonly HashSet<string> loadedUnitStateIds = new();

    [Header("Identity")]
    [SerializeField] private string saveID;
    [SerializeField] private bool generateIDFromSceneHierarchy = true;
    private UnitData unitData;
    private UnitBattleParty battleParty;

    private bool existsInWorld = true;
    private bool isRestoringParty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeStatics()
    {
        loadedUnitStateIds.Clear();
        BattleRelay.EncounterDefeated -= HandleEncounterDefeated;
        BattleRelay.EncounterDefeated += HandleEncounterDefeated;
        RestoreDefeatedEncounters();
    }

    private void Awake()
    {
        if (generateIDFromSceneHierarchy)
            saveID = BuildSceneHierarchyID(transform);

        if (unitData == null &&
            TryGetComponent(out UnitExploration exploration))
        {
            unitData = exploration.GetUnitData();
        }

        TryGetComponent(out battleParty);
    }

    private void OnEnable()
    {
        if (battleParty != null)
            battleParty.PartyChanged += HandlePartyChanged;
    }

    private void Start()
    {
        Load();
    }

    public void Save()
    {
        if (!HasSaveID())
            return;

        SaveDataTransaction.SetString(
            GetPlayerPrefsKey(saveID),
            JsonUtility.ToJson(CreateSavedData())
        );
        SaveDataTransaction.Save();
    }

    public void Load()
    {
        if (!HasSaveID())
            return;

        string key = GetPlayerPrefsKey(saveID);
        if (!SaveDataTransaction.HasKey(key))
        {
            unitData?.ClearAddedSkills();
            TryRestoreBattleReturnPosition();
            Save();
            return;
        }

        SavedData savedData =
            JsonUtility.FromJson<SavedData>(
                SaveDataTransaction.GetString(key)
            );

        if (savedData == null)
            return;

        existsInWorld = savedData.existsInWorld;
        if (!existsInWorld)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        if (savedData.sceneName == gameObject.scene.name)
            RestorePosition(savedData.position);

        TryRestoreBattleReturnPosition();

        if (loadedUnitStateIds.Add(saveID))
            RestoreUnitState(savedData);

        RestoreParty(savedData);
    }

    private void TryRestoreBattleReturnPosition()
    {
        if (!CompareTag("Player") ||
            !BattleRelay.TryConsumePlayerReturnPosition(
                gameObject.scene.name,
                out Vector3 returnPosition))
        {
            return;
        }

        RestorePosition(returnPosition);
        Save();
    }

    public void SetExistsInWorld(bool exists)
    {
        existsInWorld = exists;
        Save();

        if (!exists)
            gameObject.SetActive(false);
    }

    public bool AddSkill(SkillData skill)
    {
        if (unitData == null || !unitData.AddSkill(skill))
            return false;

        Save();
        return true;
    }

    public void SetLevel(int level)
    {
        if (unitData == null)
            return;

        UnitRuntimeState.SetProgress(
            unitData,
            Mathf.Max(1, level),
            UnitRuntimeState.GetExperience(unitData));
        Save();
    }

    public static bool IsSavedInWorld(string id)
    {
        SavedData savedData = ReadSavedData(id);
        return savedData == null || savedData.existsInWorld;
    }

    public static void SetSavedExistsInWorld(string id, bool exists)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        SavedData savedData = ReadSavedData(id) ?? new SavedData();
        savedData.existsInWorld = exists;
        WriteSavedData(id, savedData);
    }

    public static string GetSavedLocation(string id, string defaultLocation)
    {
        SavedData savedData = ReadSavedData(id);

        return savedData == null ||
               string.IsNullOrWhiteSpace(savedData.locationId)
            ? defaultLocation
            : savedData.locationId;
    }

    public static void SetSavedLocation(string id, string locationId)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        SavedData savedData = ReadSavedData(id) ?? new SavedData();
        savedData.locationId = locationId;
        WriteSavedData(id, savedData);
    }

    public static void DeleteSavedData(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        SaveDataTransaction.DeleteKey(GetPlayerPrefsKey(id));
        loadedUnitStateIds.Remove(id);
        SaveDataTransaction.Save();
    }

    private SavedData CreateSavedData()
    {
        SavedData savedData = new()
        {
            existsInWorld = existsInWorld,
            sceneName = gameObject.scene.name,
            position = transform.position
        };

        if (unitData == null)
            return savedData;

        UnitRuntimeState.State state = UnitRuntimeState.GetOrCreate(unitData);
        savedData.level = state.level;
        savedData.experience = state.experience;
        savedData.currentHP = state.currentHP;
        savedData.currentMP = state.currentMP;
        savedData.attack = state.attack;
        savedData.defense = state.defense;
        savedData.speed = state.speed;
        savedData.addedSkillNames =
            unitData.AddedSkills.Select(skill => skill.SkillName).ToList();

        if (battleParty != null)
        {
            savedData.hasPartyData = true;
            savedData.partyUnitIds = battleParty.Units
                .Where(unit => unit != null)
                .Select(GetUnitId)
                .ToList();
        }

        return savedData;
    }

    private void RestorePosition(Vector3 position)
    {
        transform.position = position;

        if (TryGetComponent(out Rigidbody2D body))
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }
    }

    private void RestoreParty(SavedData savedData)
    {
        if (battleParty == null ||
            battleParty.HasCanonicalPlayerParty ||
            !savedData.hasPartyData ||
            savedData.partyUnitIds == null)
        {
            return;
        }

        Dictionary<string, UnitData> loadedUnits = BuildLoadedUnitLookup();
        List<UnitData> restoredUnits = new();

        foreach (string unitId in savedData.partyUnitIds)
        {
            if (loadedUnits.TryGetValue(unitId, out UnitData unit))
                restoredUnits.Add(unit);
        }

        if (restoredUnits.Count != savedData.partyUnitIds.Count)
            return;

        isRestoringParty = true;

        try
        {
            battleParty.SetUnits(restoredUnits);
        }
        finally
        {
            isRestoringParty = false;
        }
    }

    private void HandlePartyChanged()
    {
        if (!isRestoringParty)
            Save();
    }

    private void RestoreUnitState(SavedData savedData)
    {
        if (unitData == null)
            return;

        UnitRuntimeState.State state = UnitRuntimeState.RestoreProgress(
            unitData,
            savedData.level,
            savedData.experience);

        if (state == null)
            return;

        bool savedStatsMatchProgress =
            savedData.level == state.level &&
            savedData.experience == state.experience;

        if (savedStatsMatchProgress)
        {
            state.currentHP = savedData.currentHP;
            state.currentMP = savedData.currentMP;
            state.attack = savedData.attack;
            state.defense = savedData.defense;
            state.speed = savedData.speed;
        }

        unitData.ClearAddedSkills();
        SkillData[] loadedSkills = Resources.FindObjectsOfTypeAll<SkillData>();
        foreach (string skillName in savedData.addedSkillNames)
        {
            SkillData skill = loadedSkills.FirstOrDefault(
                candidate => candidate.SkillName == skillName
            );

            if (skill != null)
                unitData.AddSkill(skill);
        }
    }

    private static void HandleEncounterDefeated(string encounterId)
    {
        SetSavedExistsInWorld(encounterId, false);

        SavedIdList savedIds = ReadDefeatedEncounterIds();
        if (!savedIds.ids.Contains(encounterId))
        {
            savedIds.ids.Add(encounterId);
            SaveDataTransaction.SetString(
                DefeatedEncountersKey,
                JsonUtility.ToJson(savedIds)
            );
            SaveDataTransaction.Save();
        }
    }

    private static void RestoreDefeatedEncounters()
    {
        SavedIdList savedIds = ReadDefeatedEncounterIds();

        foreach (string encounterId in savedIds.ids)
            BattleRelay.RestoreDefeatedEncounter(encounterId);
    }

    private static SavedIdList ReadDefeatedEncounterIds()
    {
        if (!SaveDataTransaction.HasKey(DefeatedEncountersKey))
            return new SavedIdList();

        return JsonUtility.FromJson<SavedIdList>(
                   SaveDataTransaction.GetString(
                       DefeatedEncountersKey
                   )
               ) ??
               new SavedIdList();
    }

    private bool HasSaveID()
    {
        if (!string.IsNullOrWhiteSpace(saveID))
            return true;

        return false;
    }

    private static string BuildSceneHierarchyID(Transform target)
    {
        string path = target.GetSiblingIndex().ToString();
        Transform current = target;

        while (current.parent != null)
        {
            current = current.parent;
            path = $"{current.GetSiblingIndex()}/{path}";
        }

        return $"{target.gameObject.scene.path}:{path}";
    }

    private static SavedData ReadSavedData(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        string key = GetPlayerPrefsKey(id);
        return SaveDataTransaction.HasKey(key)
            ? JsonUtility.FromJson<SavedData>(
                SaveDataTransaction.GetString(key)
              )
            : null;
    }

    private static void WriteSavedData(string id, SavedData savedData)
    {
        SaveDataTransaction.SetString(
            GetPlayerPrefsKey(id),
            JsonUtility.ToJson(savedData)
        );
        SaveDataTransaction.Save();
    }

    private static string GetPlayerPrefsKey(string id)
    {
        return PlayerPrefsPrefix + id.Trim();
    }

    private static Dictionary<string, UnitData> BuildLoadedUnitLookup()
    {
        Dictionary<string, UnitData> unitsById = new();
        UnitData[] loadedUnits = Resources.FindObjectsOfTypeAll<UnitData>();

        foreach (UnitData unitData in loadedUnits)
        {
            if (unitData == null)
                continue;

            unitsById[GetUnitId(unitData)] = unitData;
        }

        return unitsById;
    }

    private static string GetUnitId(UnitData unitData)
    {
        return unitData.name.Trim();
    }

    private void OnDisable()
    {
        if (battleParty != null)
            battleParty.PartyChanged -= HandlePartyChanged;
    }

    private void OnDestroy()
    {
        if (gameObject.activeSelf)
            Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }
}
