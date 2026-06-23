using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransitionState
{
    private static string destinationInteractionId;
    public static string ArrivalInteractionId { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        destinationInteractionId = string.Empty;
        ArrivalInteractionId = string.Empty;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    public static void SetDestination(string interactionId)
    {
        destinationInteractionId = interactionId;
        ArrivalInteractionId = interactionId;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrWhiteSpace(destinationInteractionId))
            return;

        GameObject runnerObject = new GameObject("Scene Transition Handler");
        SceneTransitionHandler runner =
            runnerObject.AddComponent<SceneTransitionHandler>();
        runner.Begin(scene);
    }

    private static IEnumerator PlacePlayer(Scene scene)
    {
        // Allow InteriorSpawner.Start() to instantiate the destination prefab.
        yield return null;

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
            yield break;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError(
                $"Scene '{scene.name}' has no active GameObject tagged Player."
            );
            yield break;
        }

        player.transform.SetParent(destination.transform, false);
        player.transform.localPosition = destination.PlayerSpawnOffset;
        player.transform.localRotation = Quaternion.identity;

        Vector3 spawnPosition = player.transform.position;

        if (player.TryGetComponent(out Rigidbody2D body))
        {
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
        }

        if (player.TryGetComponent(out UnitMovement movement))
            movement.Stop();

        destinationInteractionId = string.Empty;
    }

    private class SceneTransitionHandler : MonoBehaviour
    {
        public void Begin(Scene scene)
        {
            StartCoroutine(Run(scene));
        }

        private IEnumerator Run(Scene scene)
        {
            yield return PlacePlayer(scene);
            Destroy(gameObject);
        }
    }
}
