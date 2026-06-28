using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Gameplay Scene")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset gameplayScene;
#endif
    [SerializeField, HideInInspector] private string gameplayScenePath;

    private void OnEnable()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnDisable()
    {
        playButton.onClick.RemoveListener(OnPlayClicked);
        settingsButton.onClick.RemoveListener(OnSettingsClicked);
        quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    private void OnPlayClicked()
    {
        if (string.IsNullOrEmpty(gameplayScenePath))
            return;

        int sceneBuildIndex =
            SceneUtility.GetBuildIndexByScenePath(gameplayScenePath);

        if (sceneBuildIndex < 0)
            return;

        SceneManager.LoadScene(sceneBuildIndex);
    }

    private void OnSettingsClicked()
    {
        GameManager.Instance.OpenSettings();
    }

    private void OnQuitClicked()
    {
        Application.Quit();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (gameplayScene == null)
        {
            gameplayScenePath = string.Empty;
            return;
        }

        gameplayScenePath = AssetDatabase.GetAssetPath(gameplayScene);
    }
#endif
}
