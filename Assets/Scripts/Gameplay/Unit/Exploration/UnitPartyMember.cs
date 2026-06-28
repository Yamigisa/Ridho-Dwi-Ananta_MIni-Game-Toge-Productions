using UnityEngine;

/// <summary>
/// Identifies an exploration unit that belongs to the player's party.
/// Party-specific behaviour can target this capability without depending on
/// player input or a concrete controller implementation.
/// </summary>
[DisallowMultipleComponent]
public sealed class UnitPartyMember : MonoBehaviour
{
}
