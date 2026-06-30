using UnityEngine;

/// <summary>
/// Minimal status effect stub (Burning, Stunned, Slowed, etc.) so gems can
/// reference something concrete. Expand with real per-turn tick logic when
/// you build out the status system — this is intentionally bare-bones.
/// </summary>
[CreateAssetMenu(menuName = "Skills/Status Effect", fileName = "NewStatusEffect")]
public class StatusEffectData : ScriptableObject
{
    public string statusName = "Status";
    [TextArea] public string description;
    public int durationTurns = 3;
    [Tooltip("Damage/heal applied per turn while active, if any. 0 = no DoT/HoT.")]
    public int perTurnPower = 0;
    public bool stacks = false;
}
