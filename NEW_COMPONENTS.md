# New Components: Rollover and DropTarget

This document describes the two new pinball components that were added to the game.

## Rollover

**Location:** `scenes/components/Placeable/Bumpers/Rollover.cs` and `Rollover.tscn`

### Description
A pass-through component that the ball can roll over. Unlike PopBumpers which are solid RigidBody2D objects, the Rollover is an Area2D that detects when the ball passes through it.

### Features
- Awards 50 points when triggered
- Ball passes through without bouncing (Area2D detection)
- Green outline visual representation (same size as PopBumpers)
- No fill color - outline only for clean appearance
- Implements ITrigger interface for chain reactions
- Can trigger adjacent bumpers when activated via Trigger() method

### Behavior
When a ball rolls over this component:
1. Detects the collision via Area2D's BodyEntered signal
2. Calls TriggerSingle() to award points and show particles
3. Does NOT cascade to neighbors (normal behavior)

## DropTarget

**Location:** `scenes/components/Placeable/Bumpers/DropTarget.cs` and `DropTarget.tscn`

### Description
A multi-target component with 3 individual targets that must all be hit to trigger a reward. The targets retract when hit and pop back up after the component triggers. The DropTarget now bounces the ball like PopBumpers and spans 2 grid cells horizontally.

### Features
- **Ball Physics**: Individual target areas handle ball collision with bounce-back physics
- **Multi-Cell Placement**: Occupies 2 horizontal grid cells with visual feedback during placement
- **Full Width Coverage**: Spans the complete width of 2 cells with larger targets (160x160 each)
- **Bouncy Targets**: Each target has StaticBody2D child with high bounce physics material
- Contains 3 separate target areas positioned across the full 2-cell width
- Each target can be hit independently by the ball and bounces the ball back
- All 3 targets must be hit to trigger the main reward (300 points)
- Targets retract visually when hit (become invisible)
- After all 3 are hit, they reset after 1 second
- Green colored visual representation (same as PopBumper)
- Implements ITrigger interface for chain reactions
- Implements IMultiCell interface for proper grid placement

### Behavior
When a ball hits any of the three individual targets:
1. Target detects collision via Area2D's BodyEntered signal
2. Ball bounces off the target due to StaticBody2D physics material
3. Target is marked as hit and becomes invisible
4. Target's collision detection is disabled
5. If all 3 targets are hit:
   - Main trigger fires (awards 300 points and shows particles)
   - After 1 second delay, all targets pop back up
   - Targets become visible and collision is re-enabled

Note: Each target has both Area2D (for hit detection) and StaticBody2D (for physics bounce) components.

### Multi-Cell Placement
- **Grid Occupation**: Spans 2 cells horizontally (implements IMultiCell interface)
- **Visual Feedback**: When hovering during placement, both occupied cells are highlighted
- **Collision Detection**: System prevents placement if any of the required cells are occupied
- **Positioning**: Component is automatically centered across both occupied cells

### Target Layout
The three targets are positioned to span the full width of 2 cells:
- Target1: -266 units on X axis (left edge of 2-cell span)
- Target2: 0 units on X axis (center)  
- Target3: +266 units on X axis (right edge of 2-cell span)
- All targets are 160x160 units in size for better coverage
- All targets positioned 80 units up from center (top portion of cells)

## Integration Changes

### IMultiCell Interface
New interface for components that span multiple grid cells:
- `Vector2I CellSize { get; }` - Size in grid cells (e.g., 2x1 for DropTarget)
- `Vector2I[] OccupiedCells { get; }` - Relative positions of all occupied cells

### ITrigger Interface
Updated to include:
- `Vector2I GridPosition { get; set; }` - Position in the placement grid
- `Vector2 GlobalPosition { get; set; }` - World position (from Node2D)

### GridManager
- Changed from `PopBumper[,]` to `ITrigger[,]` to support multiple component types
- Added `RemoveComponent(ITrigger component)` method for proper multi-cell cleanup
- Added `CanPlaceComponent(Vector2I gridPosition, IMultiCell multiCell)` validation method

### PlacementGrid
Enhanced with multi-cell placement support:
- `SetSelectedBumperScene(PackedScene)` - Sets the currently selected component for hover preview
- `ClearSelectedBumperScene()` - Clears selection and hover state
- Hover highlighting shows all cells that will be occupied by multi-cell components
- Red highlighting indicates occupied cells, green indicates available placement
- Mouse motion tracking for real-time hover feedback

### Main.cs
Updated `OnBumperSelected` to:
- Handle multi-cell component validation (check all required cells are free)
- Occupy all cells when placing multi-cell components
- Center multi-cell components across their occupied area
- Support any ITrigger type, not just PopBumper

### PostRoundUI
- Added to BumperType enum: `Rollover`, `DropTarget`
- Passes selected bumper scene to PlacementGrid for hover preview
- Clears placement grid selection when UI is shown/hidden

Both components are now available for selection in the post-round UI.

## Technical Notes

- Both components properly implement the ITrigger interface
- DropTarget now inherits from RigidBody2D (not Area2D) and implements IMultiCell for proper physics and placement
- DropTarget positioning offset is handled during placement in Main.cs rather than in the scene structure
- Rollover still inherits from Area2D for pass-through behavior
- Both support the chain trigger system via Trigger() and TriggerSingle() methods
- DropTarget uses careful closure handling in event subscriptions to avoid bugs
- Components are compatible with the existing passive system (ChainTriggerPassive)
- Multi-cell placement system is extensible for future components of different sizes
- Hover feedback provides clear visual indication of placement validity
- Grid occupation tracking properly handles multi-cell component removal and placement validation
- Component-specific positioning offsets are managed centrally in the placement system
