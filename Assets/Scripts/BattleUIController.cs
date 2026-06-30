using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Battle UI: active unit info, turn order preview, and the player's action
/// menu (Move / one button per equipped skill / End Turn). Wire all fields in
/// the Inspector to a Canvas you build in the scene — see README.md "UI" and
/// "Action Menu" sections for click-by-click setup steps.
/// </summary>
public class BattleUIController : MonoBehaviour
{
    [Header("Status display")]
    public TextMeshProUGUI activeUnitText;
    public TextMeshProUGUI turnOrderText;

    [Header("Action Menu")]
    [Tooltip("Parent panel containing Move/Skill/EndTurn buttons. Shown when it's the player's turn to choose an action, hidden while targeting.")]
    public GameObject menuPanel;
    public Button moveButton;
    public Button endTurnButton;
    [Tooltip("Empty Transform (e.g. a Vertical Layout Group) that skill buttons get instantiated into.")]
    public Transform skillButtonContainer;
    [Tooltip("A Button prefab with a TextMeshProUGUI child for the skill's label.")]
    public GameObject skillButtonPrefab;

    private BattleController _battleController;
    private readonly List<GameObject> _spawnedSkillButtons = new List<GameObject>();

    private void Start()
    {
        _battleController = FindObjectOfType<BattleController>();

        TurnManager.Instance.OnUnitTurnStart += UpdateActiveUnitDisplay;
        _battleController.OnMenuOpened += ShowMenu;
        _battleController.OnMenuClosed += HideMenu;

        if (moveButton != null) moveButton.onClick.AddListener(() => _battleController.OnMoveButtonPressed());
        if (endTurnButton != null) endTurnButton.onClick.AddListener(() => _battleController.OnEndTurnButtonPressed());

        HideMenu();
    }

    private void UpdateActiveUnitDisplay(Unit unit)
    {
        if (activeUnitText != null)
        {
            activeUnitText.text = $"{unit.unitName}\nHP: {unit.currentHP}/{unit.maxHP}  MP: {unit.currentMP}/{unit.maxMP}";
        }

        if (turnOrderText != null)
        {
            var upcoming = TurnManager.Instance.PreviewTurnOrder(5);
            turnOrderText.text = "Turn Order: " + string.Join(" -> ", upcoming.Select(u => u.unitName));
        }
    }

    /// <summary>Shows the action menu and (re)builds the skill button list for the given unit.</summary>
    private void ShowMenu(Unit unit)
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (unit == null) return;

        if (moveButton != null) moveButton.interactable = !_battleController.HasMovedThisTurn;
        if (endTurnButton != null) endTurnButton.interactable = true;

        BuildSkillButtons(unit);
    }

    private void HideMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    private void BuildSkillButtons(Unit unit)
    {
        ClearSkillButtons();
        if (skillButtonContainer == null || skillButtonPrefab == null) return;

        var skills = unit.GetEquippedSkills();
        foreach (var skill in skills)
        {
            // Capture a local copy — looping variables can't be captured directly in a closure correctly.
            EquippedSkillInstance capturedSkill = skill;

            GameObject buttonObj = Instantiate(skillButtonPrefab, skillButtonContainer);
            _spawnedSkillButtons.Add(buttonObj);

            var label = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{capturedSkill.DisplayName}\nMP {capturedSkill.FinalMpCost}";

            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                bool usable = !_battleController.HasActedThisTurn && unit.CanUseSkill(capturedSkill);
                button.interactable = usable;
                button.onClick.AddListener(() => _battleController.OnSkillButtonPressed(capturedSkill));
            }
        }
    }

    private void ClearSkillButtons()
    {
        foreach (var obj in _spawnedSkillButtons)
            if (obj != null) Destroy(obj);
        _spawnedSkillButtons.Clear();
    }
}