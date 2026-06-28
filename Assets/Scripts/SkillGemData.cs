using UnityEngine;

/// <summary>
/// Gem "color"/attribute, used the way PoE uses Str/Dex/Int — purely a
/// socket-compatibility constraint, no inherent power. A socket can require
/// a specific color, or be "Any" (e.g. white sockets).
/// </summary>
public enum GemColor
{
    Any,
    Strength,   // physical / melee / tanky skills
    Dexterity,  // ranged / speed / evasion skills
    Intelligence // magic / elemental / spells
}

public enum GemType
{
    Active,
    Support
}

/// <summary>
/// Base data shared by Active and Support gems. Create gems as assets via
/// Assets > Create > Skills > Active Gem / Support Gem.
/// </summary>
public abstract class SkillGemData : ScriptableObject
{
    [Header("Identity")]
    public string gemName = "New Gem";
    [TextArea] public string description;
    public Sprite icon;
    public GemColor color = GemColor.Any;
    public abstract GemType GemType { get; }

    [Header("Tags")]
    public SkillTag tags;

    [Header("Leveling (optional)")]
    [Tooltip("1 = base gem. Increase via gem XP; scales numeric fields in subclasses.")]
    public int level = 1;
    public int maxLevel = 20;
}
