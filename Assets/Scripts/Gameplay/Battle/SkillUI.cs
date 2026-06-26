using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillUI : MonoBehaviour
{
    [SerializeField] private GameObject skillUIPanel;
    [SerializeField] private SkillBar skillBarPrefab;
    [SerializeField] private RectTransform skillContent;
    [SerializeField] private TextMeshProUGUI skillDescriptionText;

    private UnitData currentUnit;
    private readonly List<SkillBar> spawnedSkillBars = new();
    private SkillBar hoveredSkillBar;
    private ScrollRect skillScrollRect;

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

        RefreshScrollView();
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

    private void RefreshScrollView()
    {
        if (skillContent == null)
            return;

        ScrollRect scrollRect = GetSkillScrollRect();
        EnsureViewport(scrollRect);
        ResizeContentToFitSkills(scrollRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(skillContent);

        if (scrollRect == null)
            return;

        scrollRect.velocity = Vector2.zero;
        scrollRect.verticalNormalizedPosition = 1f;
        scrollRect.scrollSensitivity = Mathf.Max(scrollRect.scrollSensitivity, 35f);
    }

    private ScrollRect GetSkillScrollRect()
    {
        if (skillScrollRect == null && skillContent != null)
            skillScrollRect = skillContent.GetComponentInParent<ScrollRect>(true);

        return skillScrollRect;
    }

    private void EnsureViewport(ScrollRect scrollRect)
    {
        if (scrollRect == null || skillContent == null)
            return;

        if (scrollRect.viewport == null)
            scrollRect.viewport = skillContent.parent as RectTransform;

        RectTransform viewport = scrollRect.viewport;
        if (viewport == null || viewport.rect.width > 1f && viewport.rect.height > 1f)
            return;

        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = scrollRect.verticalScrollbar != null
            ? new Vector2(-20f, 0f)
            : Vector2.zero;
        viewport.pivot = new Vector2(0f, 1f);
    }

    private void ResizeContentToFitSkills(ScrollRect scrollRect)
    {
        GridLayoutGroup grid = skillContent.GetComponent<GridLayoutGroup>();
        if (grid == null)
            return;

        int skillCount = spawnedSkillBars.Count;
        if (skillCount <= 0)
        {
            skillContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
            return;
        }

        float viewportWidth = scrollRect != null && scrollRect.viewport != null
            ? scrollRect.viewport.rect.width
            : skillContent.rect.width;

        float availableWidth = Mathf.Max(
            1f,
            viewportWidth - grid.padding.left - grid.padding.right);

        int columns = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
            ? Mathf.Max(1, grid.constraintCount)
            : Mathf.Max(
                1,
                Mathf.FloorToInt(
                    (availableWidth + grid.spacing.x) /
                    Mathf.Max(1f, grid.cellSize.x + grid.spacing.x)));

        int rows = grid.constraint == GridLayoutGroup.Constraint.FixedRowCount
            ? Mathf.Max(1, grid.constraintCount)
            : Mathf.CeilToInt((float)skillCount / columns);

        float contentHeight =
            grid.padding.top +
            grid.padding.bottom +
            rows * grid.cellSize.y +
            Mathf.Max(0, rows - 1) * grid.spacing.y;

        skillContent.anchorMin = new Vector2(0f, 1f);
        skillContent.anchorMax = new Vector2(1f, 1f);
        skillContent.pivot = new Vector2(0f, 1f);
        skillContent.anchoredPosition = Vector2.zero;
        skillContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
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
