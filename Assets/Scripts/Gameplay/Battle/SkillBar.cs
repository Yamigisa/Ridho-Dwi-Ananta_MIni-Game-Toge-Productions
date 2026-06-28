using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI skillName;
    [SerializeField] private TextMeshProUGUI skillCost;
    [SerializeField] private Image skillImage;
    [SerializeField] private Button useSkillButton;

    private SkillData skillData;
    private Color originalColor;

    public SkillData SkillData => skillData;
    public event Action<SkillBar> Clicked;
    public event Action<SkillBar> HoverEntered;
    public event Action<SkillBar> HoverExited;

    private void Awake()
    {
        if (useSkillButton != null)
        {
            originalColor = useSkillButton.targetGraphic.color;
            useSkillButton.onClick.AddListener(UseSkill);
        }
    }

    public void InitializeSkillBar(SkillData data)
    {
        skillData = data;

        skillName.text = skillData.SkillName;
        skillCost.text = BuildCostText(skillData);
        skillImage.sprite = skillData.Icon;

    }

    private string BuildCostText(SkillData data)
    {
        bool hasHP = data.HPCost > 0;
        bool hasMP = data.MPCost > 0;

        if (!hasHP && !hasMP)
        {
            return "";
        }

        if (hasHP && hasMP)
        {
            return $"HP : {data.HPCost}\nMP : {data.MPCost}";
        }

        if (hasHP)
        {
            return $"HP : {data.HPCost}";
        }

        return $"MP : {data.MPCost}";
    }

    public void UseSkill()
    {
        if (GameplayState.BlocksPlayerInput)
            return;

        Clicked?.Invoke(this);
    }

    public void SetSelected(bool isSelected)
    {
        if (useSkillButton == null || useSkillButton.targetGraphic == null)
            return;

        useSkillButton.targetGraphic.color = isSelected
            ? new Color(
                originalColor.r * 0.7f,
                originalColor.g * 0.7f,
                originalColor.b * 0.7f,
                originalColor.a)
            : originalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GameplayState.BlocksPlayerInput)
            return;

        HoverEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (GameplayState.BlocksPlayerInput)
            return;

        HoverExited?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (useSkillButton != null)
            useSkillButton.onClick.RemoveListener(UseSkill);

        Clicked = null;
        HoverEntered = null;
        HoverExited = null;
    }
}
