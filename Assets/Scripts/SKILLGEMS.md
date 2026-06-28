# Skill Gem System (PoE-style)

A Path of Exile-inspired socket/gem system for skills. Instead of a hardcoded
"attack" stat, units cast **Active gems** (the actual skills), which can be
modified by **Support gems** socketed in the same linked group.

## Concepts

| Concept | PoE equivalent | Here |
|---|---|---|
| Active gem | "Fireball", "Cleave" | `ActiveSkillGemData` |
| Support gem | "Added Fire Damage", "Faster Casting" | `SupportSkillGemData` |
| Linked sockets | Sockets connected by lines on gear | `GemSocketGroup` |
| Gem tags | Attack/Spell/Fire/Projectile/AoE... | `SkillTag` flags |
| Gem colors | Str/Dex/Int sockets | `GemColor` enum |
| Computed skill | "the fireball after supports" | `EquippedSkillInstance` |

## How it fits together

1. Create gem assets: **Assets > Create > Skills > Active Gem** or **Support Gem**.
2. A `Unit` has a list of `GemSocketGroup` (think: one group per weapon/armor
   slot, or just one big group for a simple unit).
3. Each group holds **one** Active gem + up to `socketCount - 1` Support gems.
4. A Support only affects the group's Active gem if `SupportSkillGemData.CanSupport()`
   passes — i.e. the active gem's tags satisfy the support's `requiredTags`
   and don't overlap its `excludedTags`.
5. At battle time, `Unit.GetEquippedSkills()` builds an `EquippedSkillInstance`
   per group — this is the *final*, modifier-applied version of the skill
   (`FinalPower`, `FinalMpCost`, `FinalMaxRange`, `FinalAreaRadius`, etc.).
6. `BattleController` uses these instances for targeting, range preview, MP
   cost, cooldowns, and applying damage/healing/status — see
   `TryCastSelectedSkill`.

## Worked example

**Active gem: "Fireball"**
- `tags`: `Spell | Fire | Projectile | AoE`
- `basePower`: 30, `mpCost`: 15, `maxRange`: 4, `areaRadius`: 1

**Support gem: "Concentrated Effect"**
- `requiredTags`: `AoE` (only works on AoE skills)
- `powerMultiplier`: 1.4, `addedAreaRadius`: -1, `mpCostMultiplier`: 1.3
- Flavor: bigger hit, smaller radius, costs more mana — classic PoE tradeoff.

**Support gem: "Faster Casting"**
- `requiredTags`: `Spell`
- `addedCooldownTurns`: -1 (if Fireball had a cooldown)

Socket Fireball + Concentrated Effect together in one `GemSocketGroup`, and
`EquippedSkillInstance` will compute: power 42 (30 × 1.4), MP 19 (15 × 1.3
rounded), area radius 0 (1 - 1). Swap in "Faster Casting" instead and you'd
get the un-amplified power back but a turn off cooldown.

**Support gem: "Elemental Focus"** (no status, pure damage support)
- `requiredTags`: `Fire | Cold | Lightning` (any one is fine — see `HasAny` usage if you want OR semantics; default `CanSupport` uses `HasAll`, so for "any elemental" gems set `requiredTags` to a single representative tag or extend `CanSupport` with an "any of these groups" rule if you need true OR logic across many tags)
- `powerMultiplier`: 1.25
- Could also set `excludedTags = SkillTag.Buff` to ensure it never lands on a buff skill by mistake.

## Why a Support gem might do nothing

If you socket a support that requires `Melee` into a group whose active gem
is a ranged spell, `CanSupport` returns false and `GetApplicableSupports()`
silently excludes it — exactly like PoE showing a support as "grey"/inactive.
This is intentional: it lets players experiment with gem combos without the
game crashing or guessing intent, and gives you room to add UI feedback
(e.g. dim the gem icon) later.

## Extending this system

- **Gem leveling**: `SkillGemData.level` exists but isn't wired to scale
  numbers yet. A natural next step: make `basePower`/`mpCost` AnimationCurves
  or formulas keyed by `level`, recalculated in `EquippedSkillInstance`.
- **OR-tag requirements**: `CanSupport` currently requires *all* `requiredTags`
  flags to be present (AND). If you want "Fire OR Cold OR Lightning" style
  requirements, add a `requiredTagGroups: List<SkillTag>` field and check
  `requiredTagGroups.Any(group => active.tags.HasAny(group))`.
  - True multi-element gems (Cinderswift, Spellbound)
- **Socket UI**: a drag-and-drop gem inventory screen, with `GemColor`-tinted
  socket slots and link lines drawn between sockets in the same group.
- **Item integration**: right now `GemSocketGroup` lives directly on `Unit`.
  Once you add an equipment/inventory system, move socket groups onto
  `EquipmentItem` instances instead, and have `Unit.GetEquippedSkills()`
  pull from currently-equipped items.
- **Status effects**: `StatusEffectData` is a bare data stub. Build a real
  `StatusEffectController` on `Unit` that ticks active statuses each turn
  (the `// TODO` in `BattleController.ApplySkillToTarget` marks the hookup point).
