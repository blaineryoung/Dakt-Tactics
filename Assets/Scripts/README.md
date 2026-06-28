# FFT-Style Tactics Skeleton — Unity Setup Guide

This is a minimal but functional skeleton for a Final Fantasy Tactics-style
tactical RPG: grid movement, charge-time turn order, and a basic attack stub.

## Files
- `Tile.cs` — single grid cell (walkable, height, occupant, highlight colors)
- `GridManager.cs` — builds the grid, BFS movement range, attack range queries
- `Unit.cs` — base unit stats (HP/MP/Move/Jump/Speed/Attack); extend for job classes
- `TurnManager.cs` — FFT-style Charge Time queue (faster units act more often)
- `BattleController.cs` — click-to-select, move, attack stub, end turn
- `BattleUIController.cs` — minimal UI: active unit info + turn order preview

## Unity Setup (one-time)

1. **Create project**: Unity 2022 LTS or newer, 3D (URP recommended but not required).
2. **Import TextMeshPro**: Window > TextMeshPro > Import TMP Essential Resources (needed by `BattleUIController`).
3. **Copy scripts**: Drop all `.cs` files into `Assets/Scripts/`.

### Build the grid
1. Create a simple `Tile` prefab: a 1x1 Cube (or flattened Plane), scaled to `(1, 0.1, 1)`.
2. Add a `Box Collider` to it (needed for raycasting in `BattleController`).
3. Add the `Tile.cs` script to it.
4. Save it as a prefab (`Assets/Prefabs/Tile.prefab`), then delete the instance from the scene.

### Scene setup
1. Create an empty GameObject named `GridManager`, add `GridManager.cs`, assign the `Tile` prefab, set `width`/`depth`.
2. Create an empty GameObject named `TurnManager`, add `TurnManager.cs`.
3. Create an empty GameObject named `BattleController`, add `BattleController.cs`, assign your main camera.
4. Make sure your Main Camera has a `Physics Raycaster`-friendly setup (default 3D camera works fine; no special component needed since we use `Physics.Raycast` directly, not Unity UI raycasting).

### Adding units
1. Create a simple `Unit` prefab: a Capsule (placeholder model) with `Unit.cs` attached.
2. Set stats in the Inspector (HP, MP, Move, Jump, Speed, Attack Range, Attack Power).
3. Toggle `isPlayerControlled` for player vs enemy units.
4. In a small setup script (or manually in `Start()` somewhere), do:
   ```csharp
   Unit u = Instantiate(unitPrefab);
   u.PlaceOnTile(GridManager.Instance.GetTile(2, 3));
   TurnManager.Instance.RegisterUnit(u);
   ```
   Do this for every unit on the field before calling `TurnManager.Instance.StartBattle()`
   (that call already happens automatically in `BattleController.Start()`).

### UI
1. Create a Canvas with two `TextMeshProUGUI` elements (active unit info, turn order).
2. Add an empty GameObject with `BattleUIController.cs`, wire the two text fields.
3. Add an "End Turn" Button, hook its `OnClick` to `BattleUIController.OnEndTurnButtonPressed`.

## How the turn system works (important design note)
Unlike a simple "everyone takes turns in a fixed loop," FFT uses **Charge Time (CT)**:
every unit accumulates CT each tick proportional to its **Speed**. Whoever reaches
the threshold first acts. This means a fast unit might act twice between two
actions from a slow unit — which is core to FFT's tactical feel (Speed is a real
stat that matters, not just a tiebreaker).

## Skill Gem System
Skills are now driven by a Path of Exile-style gem system: Active gems (the
skill itself) socketed alongside Support gems (modifiers) in linked
`GemSocketGroup`s on each `Unit`. See **`SKILLGEMS.md`** for the full design,
worked examples, and how to create new gems as ScriptableObject assets.

## Next steps to build out
- **Job/class system**: subclass `Unit` (e.g. `KnightUnit`, `BlackMageUnit`) or use a
  `JobData` ScriptableObject the `Unit` references, for stat growth + ability lists.
- **Height/elevation**: tiles already store `height`; extend movement & line-of-sight checks.
- **Real pathing visualization**: draw the actual path, not just reachable tiles.
- **AI**: replace `RunSimpleAITurn` with real target selection + movement toward target + skill use.
- **Animation**: hook unit movement/attacks into Animator state machines instead of instant teleport.
- **Status effect controller**: `StatusEffectData` exists but isn't ticked per-turn yet — see `SKILLGEMS.md`.
- **Gem inventory/socket UI**: drag-and-drop gems between sockets, color-coded slots, link visualization.

This skeleton intentionally keeps everything code-only (no prefabs/scenes included)
since those are binary Unity assets — you'll wire up the scene by hand following the
steps above, which also doubles as a good way to learn how the pieces connect.
