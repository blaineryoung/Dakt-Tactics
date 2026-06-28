# CLAUDE.md

Guidance for Claude (via Claude Code) when working in this repository.

## Project overview
A Unity (C#) tactical RPG in the style of Final Fantasy Tactics: grid-based
movement, Charge-Time (speed-driven) turn order, job-class units, and
ability-based combat. Currently a code-only skeleton — no Unity scene/prefab
assets are checked in yet; those are created by hand in the Editor following
`README.md`.

## Repo layout
```
FFTSkeleton/
  README.md           - setup steps, how the CT turn system works, next-step roadmap
  SKILLGEMS.md         - skill gem system design doc + worked examples
  Scripts/
    Tile.cs               - grid cell: walkable, height, occupant, highlight colors
    GridManager.cs         - grid generation, BFS movement range, attack range queries
    Unit.cs                 - base unit stats + socket groups, MP/cooldown handling
    TurnManager.cs            - Charge Time turn queue + turn-order preview
    BattleController.cs       - input: select/move/cast-gem-skill/end-turn
    BattleUIController.cs      - minimal TMP-based UI stub
    Skills/
      SkillTag.cs               - flags enum used for support-gem compatibility
      SkillGemData.cs            - abstract base ScriptableObject for all gems
      ActiveSkillGemData.cs       - the actual castable skill (damage/heal/range/AoE/cost)
      SupportSkillGemData.cs       - modifier gem; requires tags to socket, multiplies stats
      StatusEffectData.cs           - bare status-effect data stub (Burning, Stunned, etc.)
      GemSocketGroup.cs              - runtime linked-socket group (1 active + N supports)
      EquippedSkillInstance.cs        - calculator: active gem + supports -> final stats
```

## Core systems & conventions

- **Turn order is Charge-Time based, not round-robin.** Every unit accumulates
  `chargeTime += speed` each tick; first to hit `Unit.ChargeThreshold` (100) acts.
  Don't replace this with a simple initiative queue — speed differentials acting
  more/less often is the intended FFT feel.
- **Grid uses 4-directional BFS for movement**, Manhattan distance for attack
  range. `Tile.height` exists for future jump/elevation rules — not yet enforced
  beyond a flat tolerance check in `GridManager.GetTilesInMoveRange`.
- **Skills are gem-driven, PoE-style** (see `SKILLGEMS.md` for full detail).
  `Unit.attackPower`/`attackRangeMin`/`attackRangeMax` are explicitly marked
  legacy/fallback in code — new combat features should go through
  `ActiveSkillGemData` + `SupportSkillGemData` + `GemSocketGroup`, not those
  fields. `EquippedSkillInstance` is the only place final (post-support) skill
  numbers should be computed; don't duplicate that math elsewhere.
  - Support-gem compatibility is AND-based (`SkillTag.HasAll`) by default.
    If a feature needs OR-based tag matching, extend `SupportSkillGemData`
    rather than hacking it into `GemSocketGroup` or `BattleController`.
- **Singletons via `Instance` static property** (`GridManager.Instance`,
  `TurnManager.Instance`) — this project intentionally avoids a DI framework
  for simplicity. Keep new manager-style classes consistent with this pattern
  unless there's a good reason to change it project-wide.
- **No scene/prefab binary assets in this repo.** Scripts only. If a task needs
  a `.unity` scene or `.prefab`, note in your response that it must be created
  in the Unity Editor (or generated via a `.unity`/`.prefab` text-yaml if you're
  confident in the format) — don't silently skip it. The same applies to gem
  assets (`.asset` files created via `ActiveSkillGemData`/`SupportSkillGemData`
  `[CreateAssetMenu]`) — these are also Editor-created, not checked in here.
- **TextMeshPro is required** for `BattleUIController` — assume TMP Essentials
  are imported; don't fall back to legacy `UnityEngine.UI.Text`.

## What's intentionally stubbed (don't be surprised by these)
- `BattleController.RunSimpleAITurn` — just ends the turn immediately.
- Job classes don't exist yet — `Unit` is the only unit type so far.
- `StatusEffectData` is pure data; nothing ticks it yet (the hookup point is
  marked with a `// TODO` in `BattleController.ApplySkillToTarget`).
- Gem leveling (`SkillGemData.level`) isn't wired into any stat scaling yet.
- No animation hookup; movement is an instant `transform.position` set in
  `Unit.PlaceOnTile`.

## When extending this project
- New job classes: prefer a `JobData` ScriptableObject referenced by `Unit`
  over deep subclassing, so designers can tune stats without code changes.
- New skills: create `ActiveSkillGemData`/`SupportSkillGemData` assets, not
  new C# fields on `Unit` or `BattleController`. Tag them accurately —
  support-gem compatibility depends entirely on correct `SkillTag` usage.
- Keep `GridManager` the single source of truth for tile/occupancy queries —
  don't let other scripts maintain their own copies of grid state.
- Match existing code style: XML doc comments (`/// <summary>`) on public
  classes/methods, `[Header("...")]` grouping in Inspector-facing fields,
  `[HideInInspector]` for state that's set by code, not designers.

## Testing / verification
There is no Unity test suite yet (no Unity Test Framework assembly set up).
This project cannot be compiled or run from the command line in this
environment — there's no Unity installation here. Don't attempt to invoke
`unity` or `dotnet build` to verify compilation; review C# changes by reading
them carefully instead, and call out anything that needs verification in the
Editor.
