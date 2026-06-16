using System.Collections.Generic;
using UnityEngine;

public class UnitBattleParty : MonoBehaviour
{
    [SerializeField] private string partyName;
    [SerializeField] private List<UnitData> units;

    public string PartyName => partyName;
    public List<UnitData> Units => units;
}