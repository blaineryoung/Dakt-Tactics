using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives the core battle loop input: click a unit to select it, see its move
/// range highlighted, move, then target with its equipped gem skill
/// (TryCastSelectedSkill) or end the turn.
/// </summary>
public class BattleController : MonoBehaviour
{
    public Camera battleCamera;

    private Unit _selectedUnit;
    private List<Tile> _currentMoveRange = new List<Tile>();
    private bool _hasMovedThisTurn;
    private bool _hasActedThisTurn;
    private EquippedSkillInstance _selectedSkill;

    private void Start()
    {
        if (battleCamera == null) battleCamera = Camera.main;
        TurnManager.Instance.OnUnitTurnStart += HandleTurnStart;
        TurnManager.Instance.StartBattle();
    }

    private void HandleTurnStart(Unit unit)
    {
        _hasMovedThisTurn = false;
        _hasActedThisTurn = false;
        _selectedSkill = null;
        GridManager.Instance.ClearAllHighlights();
        _selectedUnit = null;

        unit.TickCooldowns();

        Debug.Log($"--- {unit.unitName}'s turn (Player: {unit.isPlayerControlled}) ---");

        if (!unit.isPlayerControlled)
        {
            // Placeholder for AI; replace with real decision logic.
            RunSimpleAITurn(unit);
        }
        else
        {
            SelectUnit(unit);
        }
    }

    private void Update()
    {
        if (_selectedUnit == null || !_selectedUnit.isPlayerControlled) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = battleCamera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Tile clickedTile = hit.collider.GetComponent<Tile>();
            if (clickedTile == null) return;

            if (!_hasMovedThisTurn && _currentMoveRange.Contains(clickedTile))
            {
                MoveSelectedUnitTo(clickedTile);
            }
            else if (_hasMovedThisTurn && !_hasActedThisTurn && _selectedSkill != null)
            {
                TryCastSelectedSkill(clickedTile);
            }
        }
    }

    private void SelectUnit(Unit unit)
    {
        _selectedUnit = unit;
        GridManager.Instance.ClearAllHighlights();
        _currentMoveRange = GridManager.Instance.GetTilesInMoveRange(
            unit.currentTile, unit.moveRange, unit.jumpHeight);

        foreach (var tile in _currentMoveRange)
            tile.SetHighlight(Tile.ColorMoveRange);
    }

    private void MoveSelectedUnitTo(Tile destination)
    {
        _selectedUnit.PlaceOnTile(destination);
        _hasMovedThisTurn = true;
        GridManager.Instance.ClearAllHighlights();
        _currentMoveRange.Clear();

        // Default to the unit's first equipped skill so there's always
        // something to preview range for; UI can call SelectSkill() to switch.
        if (_selectedSkill == null)
        {
            var skills = _selectedUnit.GetEquippedSkills();
            if (skills.Count > 0) _selectedSkill = skills[0];
        }

        ShowSkillRangePreview();
    }

    /// <summary>Call from a skill-bar UI button to pick which equipped gem skill is active for targeting.</summary>
    public void SelectSkill(EquippedSkillInstance skill)
    {
        _selectedSkill = skill;
        ShowSkillRangePreview();
    }

    private void ShowSkillRangePreview()
    {
        if (_selectedSkill == null || _selectedUnit == null) return;
        GridManager.Instance.ClearAllHighlights();

        var rangeTiles = GridManager.Instance.GetTilesInAttackRange(
            _selectedUnit.currentTile, _selectedSkill.FinalMinRange, _selectedSkill.FinalMaxRange);
        foreach (var tile in rangeTiles)
            tile.SetHighlight(Tile.ColorAttackRange);
    }

    /// <summary>Call when the player clicks a target tile/unit while a skill is selected.</summary>
    public void TryCastSelectedSkill(Tile targetTile)
    {
        if (_selectedUnit == null || _selectedSkill == null || targetTile == null) return;
        if (_hasActedThisTurn) return;

        Debug.Log("Attempting to cast skill: " +
                  $"{_selectedUnit.unitName} -> {_selectedSkill.DisplayName} on tile ({targetTile.x}, {targetTile.z}).");

        if (!_selectedUnit.CanUseSkill(_selectedSkill))
        {
            Debug.Log($"{_selectedUnit.unitName} cannot use {_selectedSkill.DisplayName} " +
                      $"(MP: {_selectedUnit.currentMP}/{_selectedSkill.FinalMpCost}, " +
                      $"CD remaining: {_selectedUnit.GetCooldownRemaining(_selectedSkill.baseGem)}).");
            return;
        }

        // Gather every tile in the AoE radius around the target (radius 0 = just the target tile).
        var impactTiles = new List<Tile> { targetTile };
        if (_selectedSkill.FinalAreaRadius > 0)
            impactTiles.AddRange(GridManager.Instance.GetTilesInAttackRange(targetTile, 1, _selectedSkill.FinalAreaRadius));

        _selectedUnit.PaySkillCost(_selectedSkill);

        foreach (var tile in impactTiles)
        {
            if (tile.occupant == null) continue;
            ApplySkillToTarget(_selectedSkill, tile.occupant);
        }

        _hasActedThisTurn = true;
        GridManager.Instance.ClearAllHighlights();
        EndTurn();
    }

    private void ApplySkillToTarget(EquippedSkillInstance skill, Unit target)
    {
        if (skill.IsHeal)
        {
            target.Heal(skill.FinalPower);
            Debug.Log($"{_selectedUnit.unitName} casts {skill.DisplayName} on {target.unitName}, healing {skill.FinalPower}.");
        }
        else
        {
            target.TakeDamage(skill.FinalPower);
            Debug.Log($"{_selectedUnit.unitName} casts {skill.DisplayName} on {target.unitName} for {skill.FinalPower} damage.");
        }

        foreach (var (status, chance) in skill.PossibleStatuses)
        {
            if (Random.value <= chance)
                Debug.Log($"{target.unitName} is afflicted with {status.statusName} for {status.durationTurns} turns.");
                // TODO: hook into a real StatusEffectController when that system exists.
        }
    }

    /// <summary>Call from a UI "End Turn" button.</summary>
    public void EndTurn()
    {
        GridManager.Instance.ClearAllHighlights();
        TurnManager.Instance.AdvanceToNextTurn();
    }

    private void RunSimpleAITurn(Unit aiUnit)
    {
        // Minimal placeholder: move toward the nearest player unit, then end turn.
        // Replace with real targeting/pathing/ability selection.
        Debug.Log($"{aiUnit.unitName} (AI) acts.");
        EndTurn();
    }
}
