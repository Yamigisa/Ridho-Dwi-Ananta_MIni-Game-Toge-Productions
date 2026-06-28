using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SceneLoadInteraction : Interactable
{
    [Header("Scene Transition")]
    [SerializeField] private string interactionId;
    [FormerlySerializedAs("destinationSpawnId")]
    [SerializeField] private string destinationInteractionId;
    [SerializeField] private Vector2 playerSpawnOffset;

    public string InteractionId => interactionId;
    public Vector2 PlayerSpawnOffset => playerSpawnOffset;
    public Vector3 PlayerSpawnPosition =>
        transform.TransformPoint(playerSpawnOffset);

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
            return;

        if (string.IsNullOrWhiteSpace(destinationInteractionId))
            return;

        SceneTransitionState.SetDestination(destinationInteractionId);

        if (TimelineManager.Instance != null &&
            TimelineManager.IsAnyCutscenePlaying)
        {
            TimelineManager.Instance.CompleteTimeline();
        }

        SceneManager.LoadScene(sceneName);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(PlayerSpawnPosition, 0.2f);
        Gizmos.DrawLine(transform.position, PlayerSpawnPosition);
    }
}
