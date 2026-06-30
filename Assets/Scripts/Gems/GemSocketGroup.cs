using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A linked group of sockets (e.g. on a weapon or a unit's gear). Holds at
/// most one Active gem and any number of Support gems, up to `socketCount`.
/// All sockets in a group are considered "linked" — every support in the
/// group attempts to apply to the group's active gem.
/// </summary>
[Serializable]
public class GemSocketGroup
{
    [Tooltip("How many gems (active + supports combined) this linked group can hold.")]
    public int socketCount = 4;

    [Tooltip("Per-socket color requirement. Leave empty / GemColor.Any for no restriction. " +
             "If non-empty, length should match socketCount.")]
    public List<GemColor> socketColors = new List<GemColor>();

    [SerializeField] private ActiveSkillGemData activeGem;
    [SerializeField] private List<SupportSkillGemData> supportGems = new List<SupportSkillGemData>();

    public ActiveSkillGemData ActiveGem => activeGem;
    public IReadOnlyList<SupportSkillGemData> SupportGems => supportGems;

    public int UsedSockets => (activeGem != null ? 1 : 0) + supportGems.Count;
    public int FreeSockets => Mathf.Max(0, socketCount - UsedSockets);

    public bool TrySetActiveGem(ActiveSkillGemData gem)
    {
        if (gem == null) { activeGem = null; return true; }
        if (activeGem == null && FreeSockets <= 0) return false;
        if (!ColorAllows(gem.color)) return false;
        activeGem = gem;
        return true;
    }

    public bool TryAddSupportGem(SupportSkillGemData gem)
    {
        if (gem == null || FreeSockets <= 0) return false;
        if (!ColorAllows(gem.color)) return false;
        supportGems.Add(gem);
        return true;
    }

    public bool RemoveSupportGem(SupportSkillGemData gem) => supportGems.Remove(gem);

    /// <summary>Very simple color check: at least one socket in the group must accept this color.</summary>
    private bool ColorAllows(GemColor color)
    {
        if (color == GemColor.Any) return true;
        if (socketColors == null || socketColors.Count == 0) return true; // no restriction configured
        return socketColors.Contains(GemColor.Any) || socketColors.Contains(color);
    }

    /// <summary>Supports that are actually linked to (compatible with) the current active gem.</summary>
    public List<SupportSkillGemData> GetApplicableSupports()
    {
        if (activeGem == null) return new List<SupportSkillGemData>();
        return supportGems.Where(s => s.CanSupport(activeGem)).ToList();
    }

    /// <summary>Build the runtime, fully-modified version of the active skill in this group.</summary>
    public EquippedSkillInstance BuildInstance()
    {
        if (activeGem == null) return null;
        return new EquippedSkillInstance(activeGem, GetApplicableSupports());
    }
}
