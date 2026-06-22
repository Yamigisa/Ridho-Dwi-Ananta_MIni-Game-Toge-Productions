using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransitionState
{
    private static string destinationInteractionId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        destinationInteractionId = string.Empty;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    public static void SetDestination(string interactionId)
    {
        destinationInteractionId = interactionId;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrWhiteSpace(destinationInteractionId))
            return;

        SceneLoadInteraction destination =
            Object.FindObjectsByType<SceneLoadInteraction>(
                    FindObjectsSortMode.None
                )
                .FirstOrDefault(
                    interaction =>
                        interaction.InteractionId == destinationInteractionId
                );

        if (destination == null)
        {
            Debug.LogError(
                $"Scene '{scene.name}' has no SceneLoadInteraction with ID " +
                $"'{destinationInteractionId}'."
            );
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError(
                $"Scene '{scene.name}' has no active GameObject tagged Player."
            );
            return;
        }

        Vector3 spawnPosition = destination.PlayerSpawnPosition;
        player.transform.position = spawnPosition;

        if (player.TryGetComponent(out Rigidbody2D body))
        {
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
        }

        if (player.TryGetComponent(out UnitMovement movement))
            movement.Stop();

        destinationInteractionId = string.Empty;
    }
}
