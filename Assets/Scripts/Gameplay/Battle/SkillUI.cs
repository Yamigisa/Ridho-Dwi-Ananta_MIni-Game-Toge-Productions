using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkillUI : MonoBehaviour
{
    [SerializeField] private GameObject skillUIPanel;
    [SerializeField] private SkillBar skillBarPrefab;
    [SerializeField] private RectTransform skillContent;
    [SerializeField] private TextMeshProUGUI skillDescriptionText;

    private UnitData currentUnit;
    private readonly List<SkillBar> spawnedSkillBars = new();
    private SkillBar hoveredSkillBar;

    public event Action<SkillData> SkillSelected;

    private void Awake()
    {
        CloseSkillUI();
    }

    public void OpenSkillUI(UnitData unitData)
    {
        currentUnit = unitData;
        ClearSkillBars();
        SetSkillBars();

        if (skillUIPanel != null)
            skillUIPanel.SetActive(true);
    }

    public void CloseSkillUI()
    {
        ClearSkillHighlight();

        if (skillUIPanel != null)
            skillUIPanel.SetActive(false);
    }

    private void SetSkillBars()
    {
        if (currentUnit == null || skillBarPrefab == null || skillContent == null)
            return;

        foreach (SkillData skill in currentUnit.Skills)
        {
            if (skill == null)
                continue;

            SkillBar skillBar = Instantiate(skillBarPrefab, skillContent);
            skillBar.InitializeSkillBar(skill);
            skillBar.Clicked += HandleSkillClicked;
            skillBar.HoverEntered += HandleSkillHoverEntered;
            skillBar.HoverExited += HandleSkillHoverExited;
            spawnedSkillBars.Add(skillBar);
        }
    }

    private void HandleSkillClicked(SkillBar skillBar)
    {
        if (skillBar != null)
            SkillSelected?.Invoke(skillBar.SkillData);
    }

    private void HandleSkillHoverEntered(SkillBar skillBar)
    {
        if (hoveredSkillBar != null && hoveredSkillBar != skillBar)
            hoveredSkillBar.SetSelected(false);

        hoveredSkillBar = skillBar;
        hoveredSkillBar?.SetSelected(true);

        if (skillDescriptionText != null)
            skillDescriptionText.text = skillBar != null
                ? skillBar.SkillData.Description
                : string.Empty;
    }

    private void HandleSkillHoverExited(SkillBar skillBar)
    {
        if (hoveredSkillBar != skillBar)
            return;

        ClearSkillHighlight();
    }

    private void ClearSkillHighlight()
    {
        hoveredSkillBar?.SetSelected(false);
        hoveredSkillBar = null;

        if (skillDescriptionText != null)
            skillDescriptionText.text = string.Empty;
    }

    private void ClearSkillBars()
    {
        foreach (SkillBar skillBar in spawnedSkillBars)
        {
            if (skillBar == null)
                continue;

            skillBar.Clicked -= HandleSkillClicked;
            skillBar.HoverEntered -= HandleSkillHoverEntered;
            skillBar.HoverExited -= HandleSkillHoverExited;
            Destroy(skillBar.gameObject);
        }

        spawnedSkillBars.Clear();
        ClearSkillHighlight();
    }

    private void OnDestroy()
    {
        ClearSkillBars();
        SkillSelected = null;
    }
}
