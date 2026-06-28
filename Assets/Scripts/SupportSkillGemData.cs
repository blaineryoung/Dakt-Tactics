using UnityEngine;

/// <summary>
/// A Support gem never acts on its own — it must be linked in the same socket
/// group as an Active gem, and only takes effect if the Active gem's tags
/// satisfy `requiredTags`. This is the PoE-style "Added Fire Damage requires
/// Attack or Spell" compatibility check.
/// </summary>
[CreateAssetMenu(menuName = "Skills/Support Gem", fileName = "NewSupportGem")]
public class SupportSkillGemData : SkillGemData
{
    public override GemType GemType => GemType.Support;

    [Header("Compatibility")]
    [Tooltip("The active gem must have ALL of these tags for this support to apply.")]
    public SkillTag requiredTags = SkillTag.None;
    [Tooltip("If set, the active gem must NOT have any of these tags.")]
    public SkillTag excludedTags = SkillTag.None;

    [Header("Modifiers (applied multiplicatively unless noted)")]
    public float powerMultiplier = 1.0f;     // e.g. 1.3 = "+30% damage/healing"
    public float mpCostMultiplier = 1.0f;    // e.g. 1.5 = "+50% mana cost" (common support tradeoff)
    public int addedRange = 0;
    public int addedAreaRadius = 0;
    public int addedCooldownTurns = 0;

    [Header("Tag Injection")]
    [Tooltip("Tags ORed onto the active gem's effective tags while this support is linked, e.g. adding Fire to a physical attack.")]
    public SkillTag addedTags = SkillTag.None;

    [Header("Status Injection")]
    [Tooltip("If set, this status gets applied on hit in addition to (or instead of) the active gem's own status.")]
    public StatusEffectData injectedStatus;
    [Range(0f, 1f)] public float injectedStatusChance = 1f;

    public bool CanSupport(ActiveSkillGemData active)
    {
        if (active == null) return false;
        if (!active.tags.HasAll(requiredTags)) return false;
        if (excludedTags != SkillTag.None && active.tags.HasAny(excludedTags)) return false;
        return true;
    }
}
