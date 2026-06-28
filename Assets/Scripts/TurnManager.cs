using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Implements the classic FFT "Charge Time" turn system: every unit ticks up
/// charge time each cycle based on its Speed stat; the first unit to fill its
/// bar acts next. This naturally lets faster units act more often than slower ones,
/// rather than a strict fixed turn order.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public List<Unit> allUnits = new List<Unit>();
    public Unit ActiveUnit { get; private set; }

    public System.Action<Unit> OnUnitTurnStart;

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterUnit(Unit unit)
    {
        if (!allUnits.Contains(unit)) allUnits.Add(unit);
    }

    /// <summary>
    /// Call this once at battle start, then call AdvanceToNextTurn() whenever
    /// the active unit finishes its turn.
    /// </summary>
    public void StartBattle()
    {
        AdvanceToNextTurn();
    }

    /// <summary>
    /// Ticks all living units' charge time repeatedly until exactly one unit
    /// is ready to act, then sets it as the ActiveUnit. Ties are broken by speed.
    /// </summary>
    public void AdvanceToNextTurn()
    {
        var living = allUnits.Where(u => u.IsAlive).ToList();
        if (living.Count == 0) return;

        Unit ready = null;
        int safetyLimit = 10000; // avoid infinite loop in edge cases

        while (ready == null && safetyLimit-- > 0)
        {
            Unit best = null;
            foreach (var unit in living)
            {
                if (unit.TickCharge())
                {
                    if (best == null || unit.speed > best.speed)
                        best = unit;
                }
            }
            ready = best;
        }

        ActiveUnit = ready;
        OnUnitTurnStart?.Invoke(ActiveUnit);
    }

    /// <summary>Returns a preview of the next N units likely to act, for UI display.</summary>
    public List<Unit> PreviewTurnOrder(int count)
    {
        // Simple simulation on cloned charge values, not affecting real state.
        var sim = allUnits.Where(u => u.IsAlive)
            .Select(u => (unit: u, ct: u.chargeTime))
            .ToList();

        var order = new List<Unit>();
        int guard = count * 200;

        while (order.Count < count && guard-- > 0)
        {
            for (int i = 0; i < sim.Count; i++)
            {
                var (unit, ct) = sim[i];
                ct += unit.speed;
                sim[i] = (unit, ct);
            }

            var readyIndices = sim
                .Select((s, idx) => (s, idx))
                .Where(p => p.s.ct >= Unit.ChargeThreshold)
                .OrderByDescending(p => p.s.unit.speed)
                .ToList();

            foreach (var (s, idx) in readyIndices)
            {
                order.Add(s.unit);
                sim[idx] = (s.unit, 0);
                if (order.Count >= count) break;
            }
        }

        return order;
    }
}
