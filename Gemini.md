# Pinball — Codebase Reference (for humans and LLMs)

This document summarizes the code structure, runtime flow, important files, signals/events, debugging notes and gotchas discovered while working on the project. It's written to help future contributors and LLMs that need to reason about or modify the code.

## High-level overview

A Godot (4.x) project using C# (Mono) that simulates a pinball game. The project separates responsibilities across scenes and small components. Key gameplay flow:

1. Player launches and controls the pinball with paddles.
2. Pinball collides with bumpers and other hittable objects to score and change resources (Gold, Charge, Burn, Mult).
3. When the ball is lost, a `PostRoundUI` appears and lets the player select and place a new bumper on a `PlacementGrid` before the next round.

This file documents the most important pieces of the project and the design choices you should know when editing or extending it.

## Important files & folders

- `scenes/` — scene assets.
    - `components/Placeable/Bumpers/` — placeable bumper scenes and their scripts (e.g., `PopBumper.tscn`, `PopBumper.cs`, `ChargePopBumper.cs`).
    - `components/Hittable/` — `HitboxComponent.tscn/cs` and `HittableComponent.tscn/cs` which handle detecting hits and wiring visual/score effects.
    - `components/visual/` — `ScoreParticle.tscn` and `ScoreParticle.cs` for per-bumper particle feedback.
    - `components/UI/` — `PostRoundUI.tscn/cs` and `PlacementGrid/PlacementGrid.cs` for post-round placement UI and the placement grid control.
    - `objects/` — `bumper.tscn` (legacy/other bumper scenes), `pinball.tscn`.
- `scripts/` — C# game code.
    - `game/Main.cs` — central game controller: score, round flow, showing/hiding `PostRoundUI`, placing bumpers on the grid.
    - `game/GridManager.cs` — holds `PopBumper[,] BumperGrid` and grid dimensions.
    - `objects/Pinball.cs` — pinball behavior (collision detection, ball out detection and signalling `BallOut`).
    - `components/Placeable/Bumpers/*.cs` — `PopBumper.cs` (base), and specialized bumpers inherit `PopBumper`.

## Key runtime flow and contracts

- Main game loop & round flow
    - `Main.cs` spawns the ball and listens for `Pinball.BallOut` to end a round.
    - On round end, `Main` calls `postRoundUI.ShowUI()` and shows `PlacementGrid` to allow the player to choose and place a bumper.
    - The player selects one of three bumpers in `PostRoundUI` (UI), then clicks a cell on `PlacementGrid` to attempt placement.
    - `PostRoundUI` invokes a strongly-typed C# event `OnBumperSelectedEvent` and also emits a Godot signal `BumperSelected` with the chosen bumper and grid position. `Main.OnBumperSelected` receives that and does placement validation.

- Placement contract (what `Main.OnBumperSelected` must do)
    - Validate that the chosen grid cell is empty.
    - Instantiate the `PackedScene` as a `PopBumper` (or subclass) and set its `GridPosition`.
    - Compute the cell center using the `PlacementGrid` size and `GridManager.Instance.Columns/Rows`.
    - Set the bumper's `GlobalPosition` to the computed world position and `AddChild(newBumper)` to the scene tree.
    - Hide the grid and spawn a new ball only on successful placement.

- Hittable/Hitbox contract
    - `HitboxComponent` (Area2D) connects `BodyEntered` to `OnBodyEntered` and when the `Pinball` enters, it calls `HittableComponent.RegisterHit(pinball, GlobalPosition)` on the hittable component attached to the bumper.
    - `HittableComponent.RegisterHit(...)`:
        - Applies resource-specific visuals and increments (Burn, Gold, Charge, Mult) when those components are attached.
        - Calls `Main.AddScore(...)` for per-hit score.
        - Calls `ScoreParticleComponent.Populate(...)` on the attached `ScoreParticle` to show particles.
    - Important: `ScoreParticle` is a per-bumper scene child (not a global singleton). Each bumper has its own `ScoreParticle` instance and `HittableComponent.ScoreParticleComponent` references it.

## Signals and strong-typed events

- Godot signals used:
    - `PostRoundUI` emits `BumperSelected` (legacy/compat) and also exposes a C# event `OnBumperSelectedEvent` (preferred for C# consumers).
    - `PlacementGrid` emits `GridCellClicked` with a `Vector2I` cell coordinate.
    - `Pinball` emits a static C# event `BallOut` that `Main` subscribes to.

- Prefer C# events for internal C# wiring (type safety). Godot signals are left in place for any GDScript/visual script pieces that might also use them.

## Common pitfalls & gotchas discovered

- Control nodes must be parented under a UI root (Control inside a CanvasLayer) to participate in layout and get non-zero `Size`. If you instantiate `PostRoundUI` as a raw child of a non-UI node, panels can report `Size=(0,0)` and children won't be laid out.
- Godot signal arguments and C# method signatures can mismatch: prefer exposing a strongly-typed C# event when wiring C# listeners (we added `OnBumperSelectedEvent` to `PostRoundUI` and used it in `Main`).
- Type mismatches on instantiate: `Main` expects to call `Instantiate<PopBumper>()` when placing bumpers. Make sure all placeable bumper script classes inherit from `PopBumper` (Charge/Gold/Burn bumpers were updated to derive from `PopBumper` to avoid InvalidCastException at runtime).
- Input race/click-through: when the UI hides and the player clicks to place a bumper, the placement grid can sometimes receive the mouse event before the UI completes state changes. We addressed this by toggling an `AcceptClicks` boolean on `PlacementGrid` and using a short deferred timer (0.05s) before re-enabling grid clicks after a selection.
- Avoid global broadcast for per-bumper effects: earlier code triggered neighbors and caused all bumpers to emit particles on a single hit. We changed `HitboxComponent` to call the local `HittableComponent.RegisterHit` for the bumper actually struck so only that bumper emits particles.

## Recommended editing patterns and rules for LLMs

- Always look for signal/event definitions close to the nodes that emit them. If a C# script subscribes to a signal, prefer to add or use a C# event instead of relying on dynamic signal argument order.
- When adding new placeable bumper types, ensure they:
    - Inherit from `PopBumper` if `Main` or other grid systems instantiate them as `PopBumper`.
    - Provide a `HitboxComponent` and `HittableComponent` with `ScoreParticle` assigned.
- When doing layout/UI changes, create or reuse the `UIRoot` (Control node under a `CanvasLayer`) so UI controls have predictable sizes and anchors.
- For any code that manipulates current input acceptance (mouse clicks), use `AcceptClicks` or `MouseFilter` toggles and small deferred timers to avoid click-through races.

## Short debugging checklist (fast triage)

- UI invisible or panels Size=(0,0)? → Check the node's parent: is it under a Control/CanvasLayer? If not, reparent it into the UI root.
- InvalidCastException when instantiating bumpers? → Check that the scene's root script class inherits `PopBumper` (or update the instantiation type accordingly).
- All bumpers emitting particles on one hit? → Check `HitboxComponent.OnBodyEntered` and `PopBumper.Trigger()` usage; ensure `HittableComponent.RegisterHit` is only called for the actual struck object unless a cascade is intentionally wanted.
- First-click on UI doesn't register / grid steals clicks? → Ensure `PlacementGrid.AcceptClicks` is disabled while UI shows and re-enabled with a short deferred timer after selection.

## Useful code snippets and contracts

- Place bumper in `Main.OnBumperSelected` (condensed):

    - Validate grid empty: `if (GridManager.Instance.BumperGrid[x,y] != null) return;`
    - Instantiate: `var newBumper = bumperScene.Instantiate<PopBumper>();`
    - Set grid position: `newBumper.GridPosition = new Vector2I(x,y);`
    - Compute center and position: `newBumper.GlobalPosition = placementGrid.GlobalPosition + cellCenterLocal;`
    - Add to tree: `AddChild(newBumper);`

- Local hit handling in `HitboxComponent.OnBodyEntered`:
    - `HittableComponent.RegisterHit(pinball, GlobalPosition);`

- Particle emission contract in `HittableComponent.RegisterHit`:
    - Change color via `ScoreParticleComponent.SetColor(color);`
    - Emit via `ScoreParticleComponent.Populate(score);`

## Notes for future LLMs (how to reason about changes)

- Prefer small, localized edits. When a runtime bug is reported, trace the event path (signal -> subscriber) and verify assumptions about data types and object lifetimes.
- Pay attention to scene ownership and parent-child relationships — especially for UI controls and `CanvasItem` drawing order.
- Use defensive programming: when instantiating scenes, guard with try/catch and log actionable errors rather than letting invalid casts crash the game.
- Avoid global broadcasts for effects that are visually per-instance (particles, flashes). Use per-instance components or explicit group/targets where necessary.

## Final tips

- When you add features, also add a short unit or smoke test plan in the repository to help future runs (for Godot projects this can be a test scene that exercises the feature).
- Keep signals and event names literal and documented in the same file to make them discoverable by LLMs.
- If you want, I can also add a small `DEVELOPER.md` with a checklist for adding a new bumper type (inheritance, exported nodes, scene setup) and a quick debug guide.

---

Last updated: 2025-09-27 — content generated from the recent debugging session and codewalk-through. If you want a shorter or a more example-driven README, tell me which areas to shorten or expand.


In the post round flow (and UI), I want to allow passive abilities to be obtained. To start, have one that makes all regular pop bumpers also trigger other adjacent bumpers. Create a new script and scene for this in the Passives folder. In the post round UI, put a 3 panel selection under the current 3 panel selection for passives (make the current take less space vertically).