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
    public Vector3 PlayerSpawnPosition =>
        transform.position + (Vector3)playerSpawnOffset;

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"{name} cannot load a scene without a scene name.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"Scene '{sceneName}' is not included in the Build Profile."
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(destinationInteractionId))
        {
            Debug.LogError($"{name} has no Destination Interaction ID.");
            return;
        }

        SceneTransitionState.SetDestination(destinationInteractionId);
        SceneManager.LoadScene(sceneName);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(PlayerSpawnPosition, 0.2f);
        Gizmos.DrawLine(transform.position, PlayerSpawnPosition);
    }
}
