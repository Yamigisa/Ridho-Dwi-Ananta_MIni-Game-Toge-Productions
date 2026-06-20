using UnityEngine;

[CreateAssetMenu(menuName = "UI/Popup Messages")]
public class PopupMessages : ScriptableObject
{
    [Header("Battle - Encounter")]
    public string enemyAppeared = "A group of {enemy} appeared!";

    [Header("Battle - Turn")]
    public string unitAttack = "{unit} attacks {target}!";
    public string unitDamageDealt = "{unit} dealt {amount} damage to {target}!";
    public string unitrMiss = "{unit} missed!";
    public string unitHurt = "{unit} took {amount} damage!";
    public string unitDie = "{unit} was defeated!";
    public string unitPass = "{unit} passed and recovered {hp} HP and {mp} MP.";
    public string unitGuard = "{unit} guarded and recovered {mp} MP.";

    [Header("Battle - Items")]
    public string itemUsed = "{unit} used {item}!";
    public string itemCannotUse = "{item} cannot currently be used on {unit}.";
    public string unitItem = "{unit} tried to use an item, but items are not ready yet.";
    public string itemAttributes = "{unit}'s {attributes} increased by {amount}!";

    [Header("Battle - Skills")]
    public string unitSkill = "{unit} uses {skill}!";
    public string skillMiss = "{unit}'s {skill} missed {target}!";
    public string skillCannotUse = "{unit} does not have enough HP or MP to use {skill}.";

    [Header("Battle - Escape")]
    public string fleeAttempt = "The party is trying to flee...";
    public string escapeSuccess = "Successfully escaped!";
    public string escapeFailed = "Couldn't escape!";
    public string unitFleeAttempt = "{unit} is trying to flee...";
    public string unitFleeSuccess = "{unit} fled!";
    public string unitFleeFailed = "{unit} couldn't escape!";

    [Header("Battle - End")]
    public string victory = "Victory!";
    public string defeat = "Your party was defeated...";
    public string expGained = "Gained {exp} EXP!";

    [Header("Exploration")]
    public string itemPickedUp = "Picked up {item}!";
}
