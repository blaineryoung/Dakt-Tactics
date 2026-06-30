using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives the core battle loop for the player: select a unit, then choose an
/// action from the menu — Move, a specific equipped skill, or End Turn.
/// Picking Move or a Skill enters a targeting mode where the next tile click
/// either moves the unit or casts the skill; right-click cancels back to the
/// menu. Also runs a simple AI turn for non-player units (see RunSimpleAITurn).
/// </summary>
public class BattleController : MonoBehaviour
{
    public Camera battleCamera;

    /// <summary>What the player is currently doing — drives both input handling and what the UI should show.</summary>
    public enum ActionState
    {
        Menu,               // action menu is open, waiting for Move / Skill / End Turn
        SelectingMoveTile,  // move range is highlighted, waiting for a tile click
        SelectingSkillTarget // skill range is highlighted, waiting for a tile click
    }

    public ActionState CurrentState { get; private set; } = ActionState.Menu;
    public bool HasMovedThisTurn { get; private set; }
    public bool HasActedThisTurn { get; private set; }

    /// <summary>Fired when the menu should be shown for the given unit (turn start, or after returning from Move/Skill targeting).</summary>
    public System.Action<Unit> OnMenuOpened;
    /// <summary>Fired when the menu should be hidden (entering a targeting mode).</summary>
    public System.Action OnMenuClosed;

    private Unit _selectedUnit;
    private List<Tile> _currentMoveRange = new List<Tile>();
    private EquippedSkillInstance _selectedSkill;

    private void Start()
    {
        if (battleCamera == null) battleCamera = Camera.main;
        TurnManager.Instance.OnUnitTurnStart += HandleTurnStart;
        TurnManager.Instance.StartBattle();
    }

    private void HandleTurnStart(Unit unit)
    {
        HasMovedThisTurn = false;
        HasActedThisTurn = false;
        _selectedSkill = null;
        _currentMoveRange.Clear();
        GridManager.Instance.ClearAllHighlights();
        _selectedUnit = null;

        unit.TickCooldowns();

        Debug.Log($"--- {unit.unitName}'s turn (Player: {unit.isPlayerControlled}) ---");

        if (!unit.isPlayerControlled)
        {
            RunSimpleAITurn(unit);
        }
        else
        {
            _selectedUnit = unit;
            OpenMenu();
        }
    }

    /// <summary>Shows the action menu for the currently selected unit. Call this to re-open the menu (e.g. after Move/Skill, or Cancel).</summary>
    private void OpenMenu()
    {
        CurrentState = ActionState.Menu;
        GridManager.Instance.ClearAllHighlights();
        _currentMoveRange.Clear();
        _selectedSkill = null;
        OnMenuOpened?.Invoke(_selectedUnit);
    }

    private void Update()
    {
        if (_selectedUnit == null || !_selectedUnit.isPlayerControlled) return;

        // Right-click cancels out of a targeting mode back to the menu.
        if (Mouse.current.rightButton.wasPressedThisFrame && CurrentState != ActionState.Menu)
        {
            OpenMenu();
            return;
        }

        if (CurrentState == ActionState.Menu) return; // input handled by UI buttons, not clicks
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Ray ray = battleCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        Tile clickedTile = hit.collider.GetComponent<Tile>();
        if (clickedTile == null) return;

        if (CurrentState == ActionState.SelectingMoveTile && _currentMoveRange.Contains(clickedTile))
        {
            MoveSelectedUnitTo(clickedTile);
        }
        else if (CurrentState == ActionState.SelectingSkillTarget)
        {
            TryCastSelectedSkill(clickedTile);
        }
    }

    // ---------------- Menu actions (call these from UI buttons) ----------------

    /// <summary>Call from the menu's "Move" button.</summary>
    public void OnMoveButtonPressed()
    {
        if (_selectedUnit == null || HasMovedThisTurn) return;

        CurrentState = ActionState.SelectingMoveTile;
        OnMenuClosed?.Invoke();

        GridManager.Instance.ClearAllHighlights();
        _currentMoveRange = GridManager.Instance.GetTilesInMoveRange(
            _selectedUnit.currentTile, _selectedUnit.moveRange, _selectedUnit.jumpHeight);

        foreach (var tile in _currentMoveRange)
            tile.SetHighlight(Tile.ColorMoveRange);
    }

    /// <summary>Call from a skill button in the menu's skill list to target and cast that specific equipped skill.</summary>
    public void OnSkillButtonPressed(EquippedSkillInstance skill)
    {
        if (_selectedUnit == null || skill == null || HasActedThisTurn) return;

        _selectedSkill = skill;
        CurrentState = ActionState.SelectingSkillTarget;
        OnMenuClosed?.Invoke();
        ShowSkillRangePreview();
    }

    /// <summary>Call from the menu's "End Turn" button.</summary>
    public void OnEndTurnButtonPressed() => EndTurn();

    // ---------------- Internal action execution ----------------

    private void MoveSelectedUnitTo(Tile destination)
    {
        _selectedUnit.PlaceOnTile(destination);
        HasMovedThisTurn = true;
        OpenMenu(); // return to the menu so the player can pick a skill or end turn
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

    /// <summary>Casts the currently selected skill at the given target tile. Used by both player targeting clicks and the AI.</summary>
    private void TryCastSelectedSkill(Tile targetTile)
    {
        if (_selectedUnit == null || _selectedSkill == null || targetTile == null) return;
        if (HasActedThisTurn) return;

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
            ApplySkillToTarget(_selectedUnit, _selectedSkill, tile.occupant);
        }

        HasActedThisTurn = true;
        GridManager.Instance.ClearAllHighlights();
        EndTurn();
    }

    private void ApplySkillToTarget(Unit caster, EquippedSkillInstance skill, Unit target)
    {
        if (skill.IsHeal)
        {
            target.Heal(skill.FinalPower);
            Debug.Log($"{caster.unitName} casts {skill.DisplayName} on {target.unitName}, healing {skill.FinalPower}.");
        }
        else
        {
            target.TakeDamage(skill.FinalPower);
            Debug.Log($"{caster.unitName} casts {skill.DisplayName} on {target.unitName} for {skill.FinalPower} damage.");
        }

        foreach (var (status, chance) in skill.PossibleStatuses)
        {
            if (Random.value <= chance)
                Debug.Log($"{target.unitName} is afflicted with {status.statusName} for {status.durationTurns} turns.");
            // TODO: hook into a real StatusEffectController when that system exists.
        }
    }

    /// <summary>Ends the current unit's turn and advances the Charge Time queue.</summary>
    public void EndTurn()
    {
        GridManager.Instance.ClearAllHighlights();
        _selectedUnit = null;
        CurrentState = ActionState.Menu;
        TurnManager.Instance.AdvanceToNextTurn();
    }

    // ---------------- AI ----------------

    private void RunSimpleAITurn(Unit aiUnit)
    {
        Debug.Log($"{aiUnit.unitName} (AI) acts.");

        Unit target = FindNearestPlayerUnit(aiUnit);
        if (target == null)
        {
            Debug.Log($"{aiUnit.unitName} (AI) finds no player units alive. Ending turn.");
            EndTurn();
            return;
        }

        // --- Move toward the target ---
        var reachableTiles = GridManager.Instance.GetTilesInMoveRange(
            aiUnit.currentTile, aiUnit.moveRange, aiUnit.jumpHeight);

        Tile bestTile = aiUnit.currentTile;
        int bestDist = ManhattanDistance(aiUnit.currentTile, target.currentTile);

        foreach (var tile in reachableTiles)
        {
            int dist = ManhattanDistance(tile, target.currentTile);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTile = tile;
            }
        }

        if (bestTile != aiUnit.currentTile)
        {
            aiUnit.PlaceOnTile(bestTile);
            Debug.Log($"{aiUnit.unitName} (AI) moves toward {target.unitName}.");
        }

        // --- Cast the first equipped skill at the target, if in range ---
        var skills = aiUnit.GetEquippedSkills();
        if (skills.Count == 0)
        {
            Debug.Log($"{aiUnit.unitName} (AI) has no equipped skills. Ending turn.");
            EndTurn();
            return;
        }

        EquippedSkillInstance skill = skills[0];
        int distToTarget = ManhattanDistance(aiUnit.currentTile, target.currentTile);
        bool inRange = distToTarget >= skill.FinalMinRange && distToTarget <= skill.FinalMaxRange;

        if (inRange && aiUnit.CanUseSkill(skill))
        {
            // AI bypasses the menu/click flow entirely and casts directly.
            _selectedUnit = aiUnit;
            _selectedSkill = skill;
            TryCastSelectedSkill(target.currentTile); // this also calls EndTurn() internally
        }
        else
        {
            Debug.Log($"{aiUnit.unitName} (AI) can't reach {target.unitName} with {skill.DisplayName} this turn. Ending turn.");
            EndTurn();
        }
    }

    private Unit FindNearestPlayerUnit(Unit from)
    {
        Unit nearest = null;
        int bestDist = int.MaxValue;

        foreach (var unit in TurnManager.Instance.allUnits)
        {
            if (!unit.IsAlive || !unit.isPlayerControlled) continue;
            int dist = ManhattanDistance(from.currentTile, unit.currentTile);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = unit;
            }
        }

        return nearest;
    }

    private static int ManhattanDistance(Tile a, Tile b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
}