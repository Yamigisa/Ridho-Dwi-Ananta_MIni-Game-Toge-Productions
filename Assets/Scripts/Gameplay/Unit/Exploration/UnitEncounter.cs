using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UnitBattleParty))]
public class UnitEncounter : MonoBehaviour
{
    private UnitBattleParty enemyBattleParty;

    private void Awake()
    {
        enemyBattleParty = GetComponent<UnitBattleParty>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        UnitBattleParty playerParty = other.GetComponentInParent<UnitBattleParty>();

        if (playerParty == null)
        {
            Debug.LogWarning("No UnitBattleParty found on player!");
            return;
        }

        BattleRelay.Set(playerParty, enemyBattleParty);
        SceneManager.LoadScene("Battle");
    }
}
