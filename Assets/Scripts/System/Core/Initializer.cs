using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Initializer : MonoBehaviour
{
    [Header("Manager Prefabs")]
    [SerializeField] private AudioManager audioManagerPrefab;
    [SerializeField] private GameManager gameManagerPrefab;

    [Header("Next Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset mainMenuScene;
#endif

    [SerializeField, HideInInspector] private string mainMenuScenePath;

    private void Start()
    {
        SpawnManagers();
        LoadMainMenuScene();
    }

    private void SpawnManagers()
    {
        if (AudioManager.Instance == null && audioManagerPrefab != null)
        {
            GameObject audioManager = Instantiate(audioManagerPrefab.gameObject);
            DontDestroyOnLoad(audioManager);
        }

        if (GameManager.Instance == null && gameManagerPrefab != null)
        {
            GameObject gameManager = Instantiate(gameManagerPrefab.gameObject);
            DontDestroyOnLoad(gameManager);
        }
    }

    private void LoadMainMenuScene()
    {
        if (string.IsNullOrEmpty(mainMenuScenePath))
        {
            Debug.LogError("Main Menu Scene belum dipilih pada Initializer.");
            return;
        }

        int sceneBuildIndex =
            SceneUtility.GetBuildIndexByScenePath(mainMenuScenePath);

        if (sceneBuildIndex < 0)
        {
            Debug.LogError(
                $"Scene '{mainMenuScenePath}' belum dimasukkan ke Build Settings."
            );
            return;
        }

        SceneManager.LoadScene(sceneBuildIndex);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (mainMenuScene == null)
        {
            mainMenuScenePath = string.Empty;
            return;
        }

        mainMenuScenePath = AssetDatabase.GetAssetPath(mainMenuScene);
    }
#endif
}