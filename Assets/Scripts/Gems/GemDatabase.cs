using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Central registry of all gem assets in the game, so code can look up a gem
/// by name instead of needing a direct Inspector reference or a Resources
/// folder path. Create one instance via Assets > Create > Skills > Gem Database,
/// then drag every Active/Support gem asset you make into its lists.
/// </summary>
[CreateAssetMenu(menuName = "Skills/Gem Database", fileName = "GemDatabase")]
public class GemDatabase : ScriptableObject
{
    [Header("All gems available in the game")]
    public List<ActiveSkillGemData> activeGems = new List<ActiveSkillGemData>();
    public List<SupportSkillGemData> supportGems = new List<SupportSkillGemData>();

    private Dictionary<string, ActiveSkillGemData> _activeLookup;
    private Dictionary<string, SupportSkillGemData> _supportLookup;

    /// <summary>Builds (or rebuilds) the name->gem lookup tables. Call once before first use, e.g. in a bootstrap script.</summary>
    public void BuildLookup()
    {
        _activeLookup = activeGems
            .Where(g => g != null)
            .GroupBy(g => g.gemName)
            .ToDictionary(group => group.Key, group => group.First());

        _supportLookup = supportGems
            .Where(g => g != null)
            .GroupBy(g => g.gemName)
            .ToDictionary(group => group.Key, group => group.First());
    }

    public ActiveSkillGemData FindActive(string gemName)
    {
        if (_activeLookup == null) BuildLookup();
        return _activeLookup.TryGetValue(gemName, out var gem) ? gem : null;
    }

    public SupportSkillGemData FindSupport(string gemName)
    {
        if (_supportLookup == null) BuildLookup();
        return _supportLookup.TryGetValue(gemName, out var gem) ? gem : null;
    }

    /// <summary>Looks up either an active or support gem by name, regardless of type — handy for generic "give player a gem" flows.</summary>
    public SkillGemData FindAny(string gemName)
    {
        return (SkillGemData)FindActive(gemName) ?? FindSupport(gemName);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only helper: scans the whole project for gem assets and adds any
    /// missing ones to this database. Right-click the GemDatabase asset's
    /// component header and choose this context menu item, or call manually.
    /// </summary>
    [ContextMenu("Auto-Populate From Project")]
    private void AutoPopulateFromProject()
    {
        var activeGuids = UnityEditor.AssetDatabase.FindAssets("t:ActiveSkillGemData");
        foreach (var guid in activeGuids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var gem = UnityEditor.AssetDatabase.LoadAssetAtPath<ActiveSkillGemData>(path);
            if (gem != null && !activeGems.Contains(gem)) activeGems.Add(gem);
        }

        var supportGuids = UnityEditor.AssetDatabase.FindAssets("t:SupportSkillGemData");
        foreach (var guid in supportGuids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var gem = UnityEditor.AssetDatabase.LoadAssetAtPath<SupportSkillGemData>(path);
            if (gem != null && !supportGems.Contains(gem)) supportGems.Add(gem);
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"GemDatabase populated: {activeGems.Count} active gems, {supportGems.Count} support gems.");
    }
#endif
}