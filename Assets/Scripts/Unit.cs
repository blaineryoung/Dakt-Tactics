using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base unit class. Job classes (Knight, Black Mage, Archer, etc.) should
/// extend this with their own ability sets and stat growth later — for now
/// it just holds the core stats every FFT-style unit needs.
/// </summary>
public class Unit : MonoBehaviour
{
    [Header("Identity")]
    public string unitName = "Unit";
    public bool isPlayerControlled = true;

    [Header("Core Stats")]
    public int maxHP = 100;
    public int currentHP;
    public int maxMP = 50;
    public int currentMP;
    public int moveRange = 3;
    public int jumpHeight = 1;
    public int speed = 10;       // determines turn frequency (higher = acts more often)

    [Header("Legacy Flat Attack (superseded by Skill Gems below — kept as fallback/reference)")]
    public int attackRangeMin = 1;
    public int attackRangeMax = 1;
    public int attackPower = 15;

    [Header("Turn/Charge Time")]
    [HideInInspector] public int chargeTime = 0; // fills up to ChargeThreshold, then unit acts
    public const int ChargeThreshold = 100;

    [HideInInspector] public Tile currentTile;

    [Header("Skill Gems")]
    [Tooltip("Each entry is a linked socket group (e.g. one per equipped weapon/armor piece).")]
    public List<GemSocketGroup> socketGroups = new List<GemSocketGroup>();

    // Cooldown tracking keyed by the active gem asset, so the same gem on
    // cooldown is shared across socket groups that happen to use it twice.
    private readonly Dictionary<ActiveSkillGemData, int> _cooldowns = new Dictionary<ActiveSkillGemData, int>();

    private void Awake()
    {
        currentHP = maxHP;
        currentMP = maxMP;
    }

    /// <summary>All currently equipped, fully-modified skills (one per socket group with an active gem set).</summary>
    public List<EquippedSkillInstance> GetEquippedSkills()
    {
        var result = new List<EquippedSkillInstance>();
        foreach (var group in socketGroups)
        {
            var instance = group.BuildInstance();
            if (instance != null) result.Add(instance);
        }
        return result;
    }

    public bool IsOnCooldown(ActiveSkillGemData gem) =>
        _cooldowns.TryGetValue(gem, out int remaining) && remaining > 0;

    public int GetCooldownRemaining(ActiveSkillGemData gem) =>
        _cooldowns.TryGetValue(gem, out int remaining) ? remaining : 0;

    public bool CanUseSkill(EquippedSkillInstance skill) =>
        skill != null && currentMP >= skill.FinalMpCost && !IsOnCooldown(skill.baseGem);

    /// <summary>Spends MP and starts the gem's cooldown. Call this when the skill is actually cast.</summary>
    public void PaySkillCost(EquippedSkillInstance skill)
    {
        currentMP = Mathf.Max(0, currentMP - skill.FinalMpCost);
        if (skill.FinalCooldown > 0)
            _cooldowns[skill.baseGem] = skill.FinalCooldown;
    }

    /// <summary>Call once per turn this unit takes, to count down active gem cooldowns.</summary>
    public void TickCooldowns()
    {
        var keys = new List<ActiveSkillGemData>(_cooldowns.Keys);
        foreach (var key in keys)
        {
            if (_cooldowns[key] > 0) _cooldowns[key]--;
        }
    }

    public bool IsAlive => currentHP > 0;

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        if (currentHP == 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
    }

    protected virtual void Die()
    {
        if (currentTile != null) currentTile.occupant = null;
        gameObject.SetActive(false); // swap for a death animation later
    }

    /// <summary>Moves this unit's logical position to a new tile (visual movement handled separately).</summary>
    public void PlaceOnTile(Tile tile)
    {
        if (currentTile != null) currentTile.occupant = null;
        currentTile = tile;
        tile.occupant = this;
        transform.position = tile.transform.position + Vector3.up * 0.5f;
    }

    /// <summary>Advances this unit's charge time by its speed. Returns true if it's ready to act.</summary>
    public bool TickCharge()
    {
        chargeTime += speed;
        if (chargeTime >= ChargeThreshold)
        {
            chargeTime = 0;
            return true;
        }
        return false;
    }
}
