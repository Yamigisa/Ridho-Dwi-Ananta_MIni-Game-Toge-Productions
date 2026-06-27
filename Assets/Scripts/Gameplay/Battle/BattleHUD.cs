using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleHUD : MonoBehaviour
{
    [Header("Action Menu")]
    [SerializeField] private GameObject actionMenu;

    [Header("Action Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button defendButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button passButton;
    [SerializeField] private Button fleeButton;

    [Header("Cancel Button")]
    [SerializeField] private Button cancelButton;

    public event Action<BattleAction> OnActionSelected;
    public event Action OnCancelSelected;

    private void Awake()
    {
        attackButton.onClick.AddListener(() => SelectAction(BattleAction.Attack));
        defendButton.onClick.AddListener(() => SelectAction(BattleAction.Defend));
        itemButton.onClick.AddListener(() => SelectAction(BattleAction.Item));
        fleeButton.onClick.AddListener(() => SelectAction(BattleAction.Flee));
        skillButton.onClick.AddListener(() => SelectAction(BattleAction.Skill));
        passButton.onClick.AddListener(() => SelectAction(BattleAction.Pass));

        if (cancelButton != null)
            cancelButton.onClick.AddListener(SelectCancel);

        HideCancelButton();
    }

    private void SelectAction(BattleAction action)
    {
        if (!DialogueManager.IsGameplayInputLocked)
            OnActionSelected?.Invoke(action);
    }

    private void SelectCancel()
    {
        if (!DialogueManager.IsGameplayInputLocked)
            OnCancelSelected?.Invoke();
    }

    public void ShowActionMenu() => actionMenu.SetActive(true);
    public void HideActionMenu() => actionMenu.SetActive(false);
    public void ShowCancelButton()
    {
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(true);
    }

    public void HideCancelButton()
    {
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(false);
    }
}

public enum BattleAction
{
    Attack,
    Skill,
    Defend,
    Item,
    Pass,
    Flee
}
