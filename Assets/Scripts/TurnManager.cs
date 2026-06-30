using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Scripts.Exceptions;

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

        int safetyLimit = 10000; // avoid infinite loop in edge cases

        bool foundUnit = false;
        // Tick all units until one is ready to act, breaking ties by speed.
        while (foundUnit == false && safetyLimit-- > 0)
        {
            // Handle ticks first.  Keep doing that until at least one unit is ready to act.
            foreach (var unit in living)
            {
                if (unit.TickCharge())
                {
                    foundUnit = true;
                }
            }
        }

        // Shouldn't happen, but just in case, if we ticked a lot and no unit is ready, throw an exception.
        if (!foundUnit)
        {
            Debug.LogError("No unit became ready to act after ticking charge time.");
            throw new NoActiveUnitFoundException("No unit became ready to act after ticking charge time.");
        }

        IEnumerable<Unit> readyUnits = living.Where(u => u.IsReadyToAct);
        if (readyUnits.Count() == 0)
        {
            Debug.LogError("Units ticked an allegedly found, but the list is empty.  Probably a race condition, sucks to be you.");
            throw new NoActiveUnitFoundException("Units ticked an allegedly found, but the list is empty.  Probably a race condition, sucks to be you.");
        }

        ActiveUnit = PickNextUnit(readyUnits.ToList(), ActiveUnit, new System.Random());
        Debug.Log($"Next unit to act: {ActiveUnit.unitName}-{ActiveUnit.UnitId} (Charge: {ActiveUnit.chargeTime}, Speed: {ActiveUnit.speed})");

        OnUnitTurnStart?.Invoke(ActiveUnit);
    }

    /// <summary>
    /// Now we pick the unit to act next.  The tiebreaker criteria is:
    /// 1. Highest current charge.
    /// 2. Highest speed.
    /// 3. A unit that hasn't gone yet this round.
    /// 4. Randomly pick one if all else fails.
    /// </summary>
    /// <param name="candidates">Units that are ready to act.</param>
    /// <param name="rng"></param>
    /// <returns>The Unit that should go next.</returns>
    private Unit PickNextUnit(List<Unit> candidates, Unit activeUnit, System.Random rng)
    {
        // 1. Highest current charge
        int maxCharge = candidates.Max(u => u.chargeTime);
        var chargeFiltered = candidates.Where(u => u.chargeTime == maxCharge).ToList();
        if (chargeFiltered.Count == 1)
            return chargeFiltered[0];

        // 2. Highest speed
        int maxSpeed = chargeFiltered.Max(u => u.speed);
        var speedFiltered = chargeFiltered.Where(u => u.speed == maxSpeed).ToList();
        if (speedFiltered.Count == 1)
            return speedFiltered[0];

        // 3. A unit that hasn't gone yet this round
        var notActed = speedFiltered.Where(u => u != ActiveUnit).ToList();
        if (notActed.Count == 1)
            return notActed[0];
        if (notActed.Count > 1)
            speedFiltered = notActed;

        // 4. Randomly pick one if all else fails
        int idx = rng.Next(speedFiltered.Count);
        return speedFiltered[idx];
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
