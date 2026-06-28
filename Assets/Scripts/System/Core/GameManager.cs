using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
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
#endif
    [SerializeField, HideInInspector] private string menuScenePath;

    private bool isGamePaused = false;
    private bool isGameOver;
    private int lastPauseRequestFrame = -1;

    public static GameManager Instance { get; private set; }
    public bool IsGamePaused => isGamePaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

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
        GameInputEvents.PauseRequested += HandlePauseRequested;
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
        GameInputEvents.PauseRequested -= HandlePauseRequested;
        resumeButton.onClick.RemoveListener(TogglePause);
        settingsButton.onClick.RemoveListener(OpenSettings);
        menuButton.onClick.RemoveListener(Menu);
        closeSettingsButton.onClick.RemoveListener(CloseSettings);

        musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);
        toggleMusicButton.onClick.RemoveListener(OnToggleMusic);
        toggleSFXButton.onClick.RemoveListener(OnToggleSFX);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void TogglePause()
    {
        if (isGameOver)
            return;

        isGamePaused = !isGamePaused;

        gameText.text = "PAUSED";

        gamePanel.SetActive(isGamePaused);
        if (!isGamePaused)
            settingPanel.SetActive(false);

        resumeButton.gameObject.SetActive(true);
        settingsButton.gameObject.SetActive(true);
        menuButton.gameObject.SetActive(true);

        Time.timeScale = isGamePaused ? 0f : 1f;
    }

    private void HandlePauseRequested()
    {
        if (lastPauseRequestFrame == Time.frameCount)
            return;

        lastPauseRequestFrame = Time.frameCount;
        TogglePause();
    }

    public void GameOver()
    {
        Time.timeScale = 0f;
        isGamePaused = true;
        isGameOver = true;

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
        isGameOver = false;

        gamePanel.SetActive(false);
        settingPanel.SetActive(false);

        LoadConfiguredScene(menuScenePath);
    }

    private static void LoadConfiguredScene(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return;

        int sceneBuildIndex =
            SceneUtility.GetBuildIndexByScenePath(scenePath);

        if (sceneBuildIndex < 0)
            return;

        SceneManager.LoadScene(sceneBuildIndex);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        menuScenePath = menuScene != null
            ? AssetDatabase.GetAssetPath(menuScene)
            : string.Empty;
    }
#endif
}

/// <summary>
/// Centralizes the application-level rules that temporarily block gameplay.
/// Gameplay components depend on these rules instead of depending directly on
/// the dialogue, timeline, or pause implementations that produce them.
/// </summary>
public static class GameplayState
{
    public static bool BlocksPlayerInput =>
        DialogueManager.IsGameplayInputLocked ||
        GameManager.Instance != null && GameManager.Instance.IsGamePaused;

    public static bool BlocksWorldSimulation =>
        DialogueManager.IsGameplayInputLocked ||
        GameManager.Instance != null && GameManager.Instance.IsGamePaused;
}

public static class GameScenes
{
    public const string Initializer = "Initializer";
    public const string MainMenu = "Main Menu";
    public const string Gameplay = "Gameplay";
    public const string Interior = "Interior";
    public const string Battle = "Battle";
}
