using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UnitBattleParty))]
public class UnitEncounter : MonoBehaviour
{
    private UnitBattleParty enemyBattleParty;
    private string encounterId;

    private void Awake()
    {
        encounterId = BuildEncounterId();
        if (BattleRelay.IsEncounterDefeated(encounterId))
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

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

        UnitSaveData playerSaveData =
            playerParty.GetComponent<UnitSaveData>();
        playerSaveData?.Save();

        BattleRelay.Set(
            playerParty,
            enemyBattleParty,
            encounterId,
            gameObject.scene.name,
            playerParty.transform.position);
        SceneManager.LoadScene("Battle");
    }

    private string BuildEncounterId()
    {
        string path = transform.GetSiblingIndex().ToString();
        Transform current = transform;

        while (current.parent != null)
        {
            current = current.parent;
            path = $"{current.GetSiblingIndex()}/{path}";
        }

        return $"{gameObject.scene.path}:{path}";
    }
}
