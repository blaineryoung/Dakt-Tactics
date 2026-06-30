using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Computed, ready-to-use form of an Active gem after all linked Support
/// gems have been applied. Build one via GemSocketGroup.BuildInstance()
/// whenever a unit's loadout changes (or cache it and rebuild on change).
/// </summary>
public class EquippedSkillInstance
{
    public readonly ActiveSkillGemData baseGem;
    public readonly List<SupportSkillGemData> supports;

    public string DisplayName => baseGem.gemName;
    public SkillTag EffectiveTags { get; private set; }
    public int FinalPower { get; private set; }
    public int FinalMpCost { get; private set; }
    public int FinalMinRange { get; private set; }
    public int FinalMaxRange { get; private set; }
    public int FinalAreaRadius { get; private set; }
    public int FinalCooldown { get; private set; }
    public bool IsHeal => baseGem.isHeal;

    /// <summary>All status effects that may be applied on hit, with their chance.</summary>
    public List<(StatusEffectData status, float chance)> PossibleStatuses { get; private set; }

    public EquippedSkillInstance(ActiveSkillGemData active, IEnumerable<SupportSkillGemData> applicableSupports)
    {
        baseGem = active;
        supports = applicableSupports.ToList();
        Recalculate();
    }

    private void Recalculate()
    {
        EffectiveTags = baseGem.tags;
        float powerMult = 1f;
        float mpMult = 1f;
        int addedRange = 0;
        int addedArea = 0;
        int addedCd = 0;

        PossibleStatuses = new List<(StatusEffectData, float)>();
        if (baseGem.appliedStatus != null)
            PossibleStatuses.Add((baseGem.appliedStatus, baseGem.statusChance));

        foreach (var support in supports)
        {
            powerMult *= support.powerMultiplier;
            mpMult *= support.mpCostMultiplier;
            addedRange += support.addedRange;
            addedArea += support.addedAreaRadius;
            addedCd += support.addedCooldownTurns;
            EffectiveTags |= support.addedTags;

            if (support.injectedStatus != null)
                PossibleStatuses.Add((support.injectedStatus, support.injectedStatusChance));
        }

        FinalPower = Mathf.RoundToInt(baseGem.basePower * powerMult);
        FinalMpCost = Mathf.Max(0, Mathf.RoundToInt(baseGem.mpCost * mpMult));
        FinalMinRange = baseGem.minRange;
        FinalMaxRange = baseGem.maxRange + addedRange;
        FinalAreaRadius = Mathf.Max(0, baseGem.areaRadius + addedArea);
        FinalCooldown = Mathf.Max(0, baseGem.cooldownTurns + addedCd);
    }

    /// <summary>Short human-readable summary, handy for tooltips/UI.</summary>
    public string GetTooltip()
    {
        string range = FinalMinRange == FinalMaxRange ? $"{FinalMaxRange}" : $"{FinalMinRange}-{FinalMaxRange}";
        string aoe = FinalAreaRadius > 0 ? $", AoE radius {FinalAreaRadius}" : "";
        string kind = IsHeal ? "Heal" : "Power";
        return $"{DisplayName}\n{kind}: {FinalPower}  MP: {FinalMpCost}  Range: {range}{aoe}" +
               (FinalCooldown > 0 ? $"  CD: {FinalCooldown}t" : "") +
               (supports.Count > 0 ? $"\nLinked: {string.Join(", ", supports.Select(s => s.gemName))}" : "");
    }
}
