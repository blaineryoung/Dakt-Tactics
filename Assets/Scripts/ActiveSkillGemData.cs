using UnityEngine;

/// <summary>
/// An Active gem is an actual usable skill (a single-target slash, a fireball,
/// a heal, a dash, etc.). It's the centerpiece of a socket group; any Support
/// gems linked to it modify these base values at runtime via EquippedSkillInstance.
/// </summary>
[CreateAssetMenu(menuName = "Skills/Active Gem", fileName = "NewActiveGem")]
public class ActiveSkillGemData : SkillGemData
{
    public override GemType GemType => GemType.Active;

    [Header("Costs & Timing")]
    public int mpCost = 10;
    public int cooldownTurns = 0; // 0 = no cooldown, usable every turn it's off CT

    [Header("Targeting")]
    public int minRange = 1;
    public int maxRange = 1;
    [Tooltip("0 = single target, 1+ = radius around the impact tile")]
    public int areaRadius = 0;
    public bool requiresLineOfSight = true;

    [Header("Effect")]
    public int basePower = 20; // damage or healing magnitude, before support modifiers
    public bool isHeal = false;
    [Tooltip("Status effect to apply on hit, if any. Leave null for none.")]
    public StatusEffectData appliedStatus;
    [Range(0f, 1f)] public float statusChance = 1f;
}
