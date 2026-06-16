using UnityEngine;
using Fungus;
using UnityEngine.SceneManagement;

public class DialogueManager : MonoBehaviour
{
    [Header("Flowchart")]
    [SerializeField] private Flowchart flowchart;

    [Header("Pop up Message")]
    [SerializeField] private PopupMessages popupMessages;
    public PopupMessages Messages => popupMessages;

    [SerializeField] private string popupBlockName = "PopupMessage";
    [SerializeField] private string popupVariableName = "PopupText";

    public static DialogueManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        FindSceneFlowchart();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindSceneFlowchart();
    }

    public void PlayDialogue(string blockName)
    {
        flowchart.ExecuteBlock(blockName);
    }

    public void ShowPopup(string message)
    {
        flowchart.SetStringVariable(popupVariableName, message);
        flowchart.ExecuteBlock(popupBlockName);
    }

    public void ShowFormattedPopup(string template, params (string key, string value)[] replacements)
    {
        string message = template;
        foreach (var (key, value) in replacements)
            message = message.Replace($"{{{key}}}", value);

        ShowPopup(message);
    }

    private void FindSceneFlowchart()
    {
        flowchart = FindFirstObjectByType<Flowchart>();
    }
}
