using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InteriorSpawner : MonoBehaviour
{
    [Serializable]
    private class InteriorDefinition
    {
        [Tooltip("Must match the destination doorway's Interaction ID.")]
        public string entranceId;

        public GameObject prefab;
    }

    [SerializeField] private List<InteriorDefinition> interiors = new();
    [SerializeField] private Transform spawnParent;

    private GameObject spawnedInterior;

    private void Start()
    {
        SpawnInterior(SceneTransitionState.ArrivalInteractionId);
    }

    public void SpawnInterior(string entranceId)
    {
        DespawnInterior();

        if (string.IsNullOrWhiteSpace(entranceId))
        {
            Debug.LogWarning(
                "InteriorSpawner received no entrance ID.",
                this
            );
            return;
        }

        InteriorDefinition definition =
            interiors.FirstOrDefault(interior => interior.entranceId == entranceId);

        if (definition == null)
        {
            Debug.LogError(
                $"InteriorSpawner has no interior registered for ID '{entranceId}'.",
                this
            );
            return;
        }

        if (definition.prefab == null)
        {
            Debug.LogError(
                $"Interior '{entranceId}' has no prefab assigned.",
                this
            );
            return;
        }

        Transform parent = spawnParent != null ? spawnParent : transform;
        spawnedInterior = Instantiate(
            definition.prefab,
            parent.position,
            parent.rotation,
            parent
        );
    }

    public void DespawnInterior()
    {
        if (spawnedInterior == null)
            return;

        Destroy(spawnedInterior);
        spawnedInterior = null;
    }

    private void OnDestroy()
    {
        DespawnInterior();
    }
}
