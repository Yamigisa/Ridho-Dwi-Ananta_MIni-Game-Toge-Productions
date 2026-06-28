using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitBattleParty : MonoBehaviour
{
    [Serializable]
    private sealed class SavedParty
    {
        public List<string> unitIds = new List<string>();
    }

    [SerializeField] private string partyName;
    [SerializeField] private List<UnitData> units = new List<UnitData>();

    private const string PlayerPartyPrefsKey = "UnitBattleParty.PlayerUnits";
    private static readonly List<UnitData> cachedPlayerUnits = new List<UnitData>();
    private static bool hasCanonicalPlayerParty;

    public string PartyName => partyName;
    public IReadOnlyList<UnitData> Units => units;
    public bool HasCanonicalPlayerParty =>
        IsPlayerParty && hasCanonicalPlayerParty;
    public event Action PartyChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeStatics()
    {
        cachedPlayerUnits.Clear();
        hasCanonicalPlayerParty = false;
    }

    private void Awake()
    {
        if (!IsPlayerParty)
            return;

        if (cachedPlayerUnits.Count > 0)
        {
            units = new List<UnitData>(cachedPlayerUnits);
            hasCanonicalPlayerParty = true;
            return;
        }

        TryLoadPlayerParty();
        if (cachedPlayerUnits.Count > 0)
            units = new List<UnitData>(cachedPlayerUnits);
    }

    public void AddUnit(UnitData unit)
    {
        if (unit == null || units.Contains(unit))
            return;

        units.Add(unit);
        PersistPlayerParty();
        PartyChanged?.Invoke();
        ShowUnitJoinedPopup(unit);
    }

    public void AddParty(UnitData unit)
    {
        AddUnit(unit);
    }

    public void AddToPlayerParty(UnitData unit)
    {
        UnitBattleParty playerParty = GetPlayerParty();

        if (playerParty == null)
        {
            Debug.LogWarning("No active player UnitBattleParty found.");
            return;
        }

        playerParty.AddUnit(unit);
    }

    public void RemoveUnit(UnitData unit)
    {
        if (unit == null || !units.Remove(unit))
            return;

        PersistPlayerParty();
        PartyChanged?.Invoke();
    }

    public void SetUnits(IEnumerable<UnitData> newUnits)
    {
        units.Clear();

        foreach (UnitData unit in newUnits)
        {
            if (unit != null && !units.Contains(unit))
                units.Add(unit);
        }

        PersistPlayerParty();
        PartyChanged?.Invoke();
    }

    private bool IsPlayerParty =>
        CompareTag("Player") ||
        string.Equals(partyName, "Player", StringComparison.OrdinalIgnoreCase);

    private void PersistPlayerParty()
    {
        if (!IsPlayerParty)
            return;

        cachedPlayerUnits.Clear();
        cachedPlayerUnits.AddRange(units);
        hasCanonicalPlayerParty = true;

        SavedParty savedParty = new SavedParty();
        foreach (UnitData unit in units)
        {
            if (unit != null)
                savedParty.unitIds.Add(GetUnitId(unit));
        }

        SaveDataTransaction.SetString(
            PlayerPartyPrefsKey,
            JsonUtility.ToJson(savedParty));
        SaveDataTransaction.Save();
    }

    private static void TryLoadPlayerParty()
    {
        if (!SaveDataTransaction.HasKey(PlayerPartyPrefsKey))
            return;

        SavedParty savedParty = JsonUtility.FromJson<SavedParty>(
            SaveDataTransaction.GetString(PlayerPartyPrefsKey));

        if (savedParty?.unitIds == null || savedParty.unitIds.Count == 0)
            return;

        Dictionary<string, UnitData> loadedUnits = BuildLoadedUnitLookup();
        List<UnitData> restoredUnits = new List<UnitData>();

        foreach (string unitId in savedParty.unitIds)
        {
            if (loadedUnits.TryGetValue(unitId, out UnitData unit) &&
                !restoredUnits.Contains(unit))
            {
                restoredUnits.Add(unit);
            }
        }

        if (restoredUnits.Count != savedParty.unitIds.Count)
            return;

        cachedPlayerUnits.AddRange(restoredUnits);
        hasCanonicalPlayerParty = true;
    }

    private static Dictionary<string, UnitData> BuildLoadedUnitLookup()
    {
        Dictionary<string, UnitData> unitsById = new Dictionary<string, UnitData>();
        UnitData[] loadedUnits = Resources.FindObjectsOfTypeAll<UnitData>();

        foreach (UnitData unit in loadedUnits)
        {
            if (unit != null)
                unitsById[GetUnitId(unit)] = unit;
        }

        return unitsById;
    }

    private static string GetUnitId(UnitData unit)
    {
        return unit.name.Trim();
    }

    private static void ShowUnitJoinedPopup(UnitData unit)
    {
        if (DialogueManager.Instance == null ||
            DialogueManager.Instance.Messages == null)
        {
            return;
        }

        string unitName = !string.IsNullOrWhiteSpace(unit.unitName)
            ? unit.unitName
            : unit.name;

        DialogueManager.Instance.ShowFormattedPopup(
            DialogueManager.Instance.Messages.unitJoinedParty,
            ("unit", unitName));
    }

    private static UnitBattleParty GetPlayerParty()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null
            ? player.GetComponentInParent<UnitBattleParty>()
            : null;
    }
}
