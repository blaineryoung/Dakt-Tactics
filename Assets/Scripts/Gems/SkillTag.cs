using System;

/// <summary>
/// Tags describe what a skill IS (damage type, delivery method, category).
/// Support gems check these tags to decide if they can socket with a given
/// Active gem — e.g. "Added Cold Damage" support requires the Attack or Spell
/// tag, "Faster Projectiles" requires the Projectile tag, etc. This mirrors
/// Path of Exile's gem tag system.
/// </summary>
[Flags]
public enum SkillTag
{
    None        = 0,
    Attack      = 1 << 0,
    Spell       = 1 << 1,
    Melee       = 1 << 2,
    Projectile  = 1 << 3,
    AoE         = 1 << 4,
    Physical    = 1 << 5,
    Fire        = 1 << 6,
    Cold        = 1 << 7,
    Lightning   = 1 << 8,
    Holy        = 1 << 9,
    Dark        = 1 << 10,
    Movement    = 1 << 11,
    Support     = 1 << 12,
    Heal        = 1 << 13,
    Buff        = 1 << 14,
    Debuff      = 1 << 15,
}

public static class SkillTagExtensions
{
    /// <summary>True if `tags` contains every flag set in `required`.</summary>
    public static bool HasAll(this SkillTag tags, SkillTag required) =>
        (tags & required) == required;

    /// <summary>True if `tags` contains at least one flag set in `anyOf`.</summary>
    public static bool HasAny(this SkillTag tags, SkillTag anyOf) =>
        anyOf == SkillTag.None || (tags & anyOf) != SkillTag.None;
}
