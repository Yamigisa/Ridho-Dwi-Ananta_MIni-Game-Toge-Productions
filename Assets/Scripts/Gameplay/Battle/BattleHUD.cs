using System;
using UnityEngine;
using UnityEngine.InputSystem;
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
        attackButton.onClick.AddListener(SelectAttack);
        defendButton.onClick.AddListener(SelectDefend);
        itemButton.onClick.AddListener(SelectItem);
        fleeButton.onClick.AddListener(SelectFlee);
        skillButton.onClick.AddListener(SelectSkill);
        passButton.onClick.AddListener(SelectPass);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(SelectCancel);

        HideCancelButton();
    }

    private void OnDestroy()
    {
        attackButton.onClick.RemoveListener(SelectAttack);
        defendButton.onClick.RemoveListener(SelectDefend);
        itemButton.onClick.RemoveListener(SelectItem);
        fleeButton.onClick.RemoveListener(SelectFlee);
        skillButton.onClick.RemoveListener(SelectSkill);
        passButton.onClick.RemoveListener(SelectPass);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(SelectCancel);

        OnActionSelected = null;
        OnCancelSelected = null;
    }

    private void Update()
    {
        if (GameplayState.BlocksPlayerInput ||
            cancelButton == null ||
            !cancelButton.gameObject.activeInHierarchy)
            return;

        bool keyboardCanceled =
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame;
        bool mouseCanceled =
            Mouse.current != null &&
            Mouse.current.rightButton.wasPressedThisFrame;

        if (keyboardCanceled || mouseCanceled)
            SelectCancel();
    }

    private void SelectAttack() => SelectAction(BattleAction.Attack);
    private void SelectSkill() => SelectAction(BattleAction.Skill);
    private void SelectDefend() => SelectAction(BattleAction.Defend);
    private void SelectItem() => SelectAction(BattleAction.Item);
    private void SelectPass() => SelectAction(BattleAction.Pass);
    private void SelectFlee() => SelectAction(BattleAction.Flee);

    private void SelectAction(BattleAction action)
    {
        if (!GameplayState.BlocksPlayerInput)
            OnActionSelected?.Invoke(action);
    }

    private void SelectCancel()
    {
        if (!GameplayState.BlocksPlayerInput)
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
