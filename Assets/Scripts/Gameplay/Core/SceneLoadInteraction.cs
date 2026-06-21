using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Interactable))]
public class SceneLoadInteraction : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset destinationScene;
#endif

    [SerializeField, HideInInspector] private string destinationScenePath;

    private Interactable interactable;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
    }

    private void OnEnable()
    {
        if (interactable == null)
            interactable = GetComponent<Interactable>();

        interactable.Interacted += HandleInteraction;
    }

    private void OnDisable()
    {
        if (interactable != null)
            interactable.Interacted -= HandleInteraction;
    }

    public void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(destinationScenePath))
        {
            Debug.LogError($"{name} has no destination scene assigned.");
            return;
        }

        int sceneBuildIndex =
            SceneUtility.GetBuildIndexByScenePath(destinationScenePath);

        if (sceneBuildIndex < 0)
        {
            Debug.LogError(
                $"Scene '{destinationScenePath}' is not included in the Build Profile."
            );
            return;
        }

        SceneManager.LoadScene(sceneBuildIndex);
    }

    private void HandleInteraction(GameObject interactor)
    {
        LoadScene();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        destinationScenePath =
            destinationScene == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(destinationScene);
    }
#endif
}
