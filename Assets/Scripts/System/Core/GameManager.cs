using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] private Button resetButton;

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
        if (resumeButton != null)
            resumeButton.onClick.AddListener(TogglePause);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (menuButton != null)
            menuButton.onClick.AddListener(Menu);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetGame);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);

        if (toggleMusicButton != null)
            toggleMusicButton.onClick.AddListener(OnToggleMusic);

        if (toggleSFXButton != null)
            toggleSFXButton.onClick.AddListener(OnToggleSFX);

        RefreshAudioUI();
    }

    private void OnDisable()
    {
        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(TogglePause);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OpenSettings);

        if (menuButton != null)
            menuButton.onClick.RemoveListener(Menu);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetGame);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.RemoveListener(CloseSettings);

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSFXSliderChanged);

        if (toggleMusicButton != null)
            toggleMusicButton.onClick.RemoveListener(OnToggleMusic);

        if (toggleSFXButton != null)
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
        resetButton.gameObject.SetActive(false);

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
        resetButton.gameObject.SetActive(true);

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
        if (settingPanel != null)
            settingPanel.SetActive(false);
    }

    private void OnMusicSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    private void OnSFXSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }

    private void OnToggleMusic()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.ToggleMusicMute();
        RefreshAudioUI();
    }

    private void OnToggleSFX()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.ToggleSFXMute();
        RefreshAudioUI();
    }

    private void RefreshAudioUI()
    {
        if (AudioManager.Instance == null) return;

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

        SceneManager.LoadScene("Main Menu");
    }

    public void ResetGame()
    {
        Time.timeScale = 1f;
        isGamePaused = false;

        gamePanel.SetActive(false);
        settingPanel.SetActive(false);

        SceneManager.LoadScene("Gameplay");
        // Add your reset scene / reset gameplay logic here later.
    }
}