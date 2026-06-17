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
    public string itemAttributes = "{unit}'s {attributes} increased by {amount}!";

    [Header("Battle - Escape")]
    public string fleeAttempt = "The party is trying to flee...";
    public string escapeSuccess = "Successfully escaped!";
    public string escapeFailed = "Couldn't escape!";

    [Header("Battle - End")]
    public string victory = "Victory!";
    public string defeat = "Your party was defeated...";
    public string expGained = "Gained {exp} EXP!";

    [Header("Exploration")]
    public string itemPickedUp = "Picked up {item}!";
}
