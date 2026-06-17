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
        attackButton.onClick.AddListener(() => OnActionSelected?.Invoke(BattleAction.Attack));
        defendButton.onClick.AddListener(() => OnActionSelected?.Invoke(BattleAction.Defend));
        itemButton.onClick.AddListener(() => OnActionSelected?.Invoke(BattleAction.Item));
        fleeButton.onClick.AddListener(() => OnActionSelected?.Invoke(BattleAction.Flee));
        skillButton.onClick.AddListener(() => OnActionSelected?.Invoke(BattleAction.Skill));
        passButton.onClick.AddListener(() => OnActionSelected?.Invoke(BattleAction.Pass));

        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => OnCancelSelected?.Invoke());

        HideCancelButton();
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
