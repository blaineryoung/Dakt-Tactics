# FFT-Style Tactics Skeleton — Unity Setup Guide

This is a minimal but functional skeleton for a Final Fantasy Tactics-style
tactical RPG: grid movement, charge-time turn order, and gem-driven skills via a player action menu.

## Files
- `Tile.cs` — single grid cell (walkable, height, occupant, highlight colors)
- `GridManager.cs` — builds the grid, BFS movement range, attack range queries
- `Unit.cs` — base unit stats (HP/MP/Move/Jump/Speed/Attack); extend for job classes
- `TurnManager.cs` — FFT-style Charge Time queue (faster units act more often)
- `BattleController.cs` — action-menu state machine: select unit, Move/Skill/EndTurn
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
4. Create a small setup script (e.g. `BattleSetup.cs`) on an empty GameObject in
   the scene, with a public field for each unit prefab you want to spawn, assigned
   in the Inspector by dragging the prefab asset onto the field:
   ```csharp
   public class BattleSetup : MonoBehaviour
   {
       public Unit unitPrefab; // assign your Unit prefab here in the Inspector

       private void Start()
       {
           Unit u = Instantiate(unitPrefab);
           u.PlaceOnTile(GridManager.Instance.GetTile(2, 3));
           TurnManager.Instance.RegisterUnit(u);
       }
   }
   ```
   Do this for every unit on the field before calling `TurnManager.Instance.StartBattle()`
   (that call already happens automatically in `BattleController.Start()`, so make
   sure this setup script's `Start()` runs first — e.g. via Script Execution Order,
   or by calling `TurnManager.Instance.RegisterUnit()` from `Awake()` instead).

### UI
1. **Create the Canvas**: right-click in the Hierarchy > UI > Canvas. This auto-creates
   an EventSystem too if one doesn't exist. Set the Canvas's `Render Mode` to
   `Screen Space - Overlay` (default) for a simple HUD that doesn't need a camera.
2. **Import TextMeshPro** if you haven't: Window > TextMeshPro > Import TMP Essential
   Resources (a popup will prompt this automatically the first time you add a TMP object).
3. **Add the two status text elements**: right-click the Canvas > UI > Text - TextMeshPro,
   twice. Rename one `ActiveUnitText` and the other `TurnOrderText`. Position them
   wherever you like (e.g. top-left and top-right) using the Rect Transform.
4. **Add the controller**: right-click in the Hierarchy > Create Empty, name it
   `BattleUI`, add the `BattleUIController.cs` component to it.
5. **Wire the status fields**: select `BattleUI`, drag `ActiveUnitText` onto the
   `Active Unit Text` field and `TurnOrderText` onto the `Turn Order Text` field.

### Action Menu (Move / Skill / End Turn)
The player's turn is driven by an action menu: a panel with a Move button, one
button per equipped skill, and an End Turn button. `BattleController` opens
this menu at the start of the player's turn and after moving, and hides it
while the player is targeting a move tile or skill tile.

1. **Create the menu panel**: right-click the Canvas > UI > Panel. Rename it
   `ActionMenuPanel`. Position it somewhere unobtrusive (e.g. bottom-center).
   Optionally add a `Vertical Layout Group` or `Horizontal Layout Group`
   component to it so buttons added later space themselves automatically.
2. **Add the Move button**: right-click `ActionMenuPanel` > UI > Button - TextMeshPro.
   Rename it `MoveButton`, change its child text to "Move".
3. **Add the End Turn button**: same as above, rename `EndTurnButton`, text "End Turn".
4. **Create a skill button prefab**: right-click `ActionMenuPanel` > UI > Button - TextMeshPro,
   rename it `SkillButton`. Drag it from the Hierarchy into a Project folder
   (e.g. `Assets/Prefabs`) to turn it into a prefab, then delete the instance
   from the Hierarchy — this prefab gets instantiated once per equipped skill
   at runtime, so it shouldn't live in the scene permanently.
5. **Create the skill button container**: right-click `ActionMenuPanel` > Create
   Empty, rename it `SkillButtonContainer`. Add a `Vertical Layout Group` (or
   `Horizontal Layout Group`) component to it so spawned skill buttons line up.
6. **Wire `BattleUI`'s Action Menu fields**: select `BattleUI` in the Hierarchy
   and drag: `ActionMenuPanel` → `Menu Panel`, `MoveButton` → `Move Button`,
   `EndTurnButton` → `End Turn Button`, `SkillButtonContainer` → `Skill Button
   Container`, and the `SkillButton` prefab asset → `Skill Button Prefab`.
   Note: unlike the old setup, you do **not** need to manually hook up
   `OnClick()` for Move or End Turn in the Inspector — `BattleUIController`
   wires those (and every spawned skill button) in code at runtime.
7. **Press Play** to test. At the start of a player unit's turn, the menu
   should appear with Move, one button per equipped skill (showing its name
   and MP cost, greyed out if unaffordable or on cooldown), and End Turn.
   Clicking Move highlights the move range and hides the menu — click a
   highlighted tile to move, which reopens the menu. Clicking a skill
   highlights its range and hides the menu — click a tile to cast, which ends
   the turn. Right-click at any point during targeting to cancel back to the menu.

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
- **AI**: `RunSimpleAITurn` now moves toward the nearest player unit and casts its
  first equipped skill if in range — extend with skill choice, target priority
  (lowest HP, highest threat), and obstacle-aware pathing.
- **Animation**: hook unit movement/attacks into Animator state machines instead of instant teleport.
- **Status effect controller**: `StatusEffectData` exists but isn't ticked per-turn yet — see `SKILLGEMS.md`.
- **Gem inventory/socket UI**: drag-and-drop gems between sockets, color-coded slots, link visualization.

This skeleton intentionally keeps everything code-only (no prefabs/scenes included)
since those are binary Unity assets — you'll wire up the scene by hand following the
steps above, which also doubles as a good way to learn how the pieces connect.