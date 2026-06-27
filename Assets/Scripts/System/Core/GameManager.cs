using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    [Header("Panel & Text Field")]
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private TextMeshProUGUI gameText;

    [Header("Pause Field")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button menuButton;

    [Header("Settings UI Field")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private Button toggleMusicButton;
    [SerializeField] private Button toggleSFXButton;
    [SerializeField] private Button closeSettingsButton;

    [SerializeField] private Image musicToggleImage;
    [SerializeField] private Image sfxToggleImage;

    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Sprite sfxOnSprite;
    [SerializeField] private Sprite sfxOffSprite;

    [Header("Scene Navigation")]
#if UNITY_EDITOR
    [SerializeField] private SceneAsset menuScene;
    [SerializeField] private SceneAsset resetGameScene;
#endif
    [SerializeField, HideInInspector] private string menuScenePath;
    [SerializeField, HideInInspector] private string resetGameScenePath;

    private bool isGamePaused = false;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        resumeButton.onClick.AddListener(TogglePause);
        settingsButton.onClick.AddListener(OpenSettings);
        menuButton.onClick.AddListener(Menu);
        closeSettingsButton.onClick.AddListener(CloseSettings);

        musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
        toggleMusicButton.onClick.AddListener(OnToggleMusic);
        toggleSFXButton.onClick.AddListener(OnToggleSFX);

        RefreshAudioUI();
    }

    private void OnDisable()
    {
        resumeButton.onClick.RemoveListener(TogglePause);
        settingsButton.onClick.RemoveListener(OpenSettings);
        menuButton.onClick.RemoveListener(Menu);
        closeSettingsButton.onClick.RemoveListener(CloseSettings);

        musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
        toggleMusicButton.onClick.RemoveListener(OnToggleMusic);
        toggleSFXButton.onClick.RemoveListener(OnToggleSFX);
    }

    public void TogglePause()
    {
        isGamePaused = !isGamePaused;

        gameText.text = "PAUSED";

        gamePanel.SetActive(isGamePaused);

        resumeButton.gameObject.SetActive(true);
        settingsButton.gameObject.SetActive(true);
        menuButton.gameObject.SetActive(true);

        Time.timeScale = isGamePaused ? 0f : 1f;
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        isGamePaused = true;

        gamePanel.SetActive(true);
        settingPanel.SetActive(false);

        gameText.text = "GAME OVER";

        resumeButton.gameObject.SetActive(false);
        settingsButton.gameObject.SetActive(false);
        menuButton.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenSettings()
    {
        settingPanel.SetActive(true);
        RefreshAudioUI();
    }

    private void CloseSettings()
    {
        settingPanel.SetActive(false);
    }

    private void OnMusicSliderChanged(float value)
    {
        AudioManager.Instance.SetMusicVolume(value);
    }

    private void OnSFXSliderChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    private void OnToggleMusic()
    {
        AudioManager.Instance.ToggleMusicMute();
        RefreshAudioUI();
    }

    private void OnToggleSFX()
    {
        AudioManager.Instance.ToggleSFXMute();
        RefreshAudioUI();
    }

    private void RefreshAudioUI()
    {
        musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
        sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SFXVolume);

        musicToggleImage.sprite = AudioManager.Instance.IsMusicMuted ? musicOffSprite : musicOnSprite;
        sfxToggleImage.sprite = AudioManager.Instance.IsSFXMuted ? sfxOffSprite : sfxOnSprite;
    }

    public void Menu()
    {
        Time.timeScale = 1f;
        isGamePaused = false;

        gamePanel.SetActive(false);
        settingPanel.SetActive(false);

        LoadConfiguredScene(menuScenePath, "Menu");
    }

    private static void LoadConfiguredScene(
        string scenePath,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            Debug.LogError(
                $"{fieldName} scene is not assigned on GameManager.");
            return;
        }

        int sceneBuildIndex =
            SceneUtility.GetBuildIndexByScenePath(scenePath);

        if (sceneBuildIndex < 0)
        {
            Debug.LogError(
                $"Scene '{scenePath}' is not included in Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneBuildIndex);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        menuScenePath = menuScene != null
            ? AssetDatabase.GetAssetPath(menuScene)
            : string.Empty;

        resetGameScenePath = resetGameScene != null
            ? AssetDatabase.GetAssetPath(resetGameScene)
            : string.Empty;
    }
#endif
}
