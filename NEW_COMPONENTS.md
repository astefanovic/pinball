# New Components: Rollover and DropTarget

This document describes the two new pinball components that were added to the game.

## Rollover

**Location:** `scenes/components/Placeable/Bumpers/Rollover.cs` and `Rollover.tscn`

### Description
A pass-through component that the ball can roll over. Unlike PopBumpers which are solid RigidBody2D objects, the Rollover is an Area2D that detects when the ball passes through it.

### Features
- Awards 50 points when triggered
- Ball passes through without bouncing (Area2D detection)
- Purple/magenta colored visual representation (smaller than PopBumpers)
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
A multi-target component with 3 individual targets that must all be hit to trigger a reward. The targets retract when hit and pop back up after the component triggers.

### Features
- Contains 3 separate target areas arranged horizontally
- Each target can be hit independently by the ball
- All 3 targets must be hit to trigger the main reward (300 points)
- Targets retract visually when hit (become invisible)
- After all 3 are hit, they reset after 1 second
- Orange colored visual representation
- Implements ITrigger interface for chain reactions

### Behavior
When a ball hits a target:
1. Target is marked as hit and becomes invisible
2. Target's collision detection is disabled
3. If all 3 targets are hit:
   - Main trigger fires (awards 300 points and shows particles)
   - After 1 second delay, all targets pop back up
   - Targets become visible and collision is re-enabled

### Target Layout
The three targets are positioned:
- Target1: -120 units on X axis (left)
- Target2: 0 units on X axis (center)
- Target3: +120 units on X axis (right)

## Integration Changes

### ITrigger Interface
Updated to include:
- `Vector2I GridPosition { get; set; }` - Position in the placement grid
- `Vector2 GlobalPosition { get; set; }` - World position (from Node2D)

### GridManager
Changed from `PopBumper[,]` to `ITrigger[,]` to support multiple component types.

### Main.cs
Updated `OnBumperSelected` to:
- Instantiate components as Node2D first
- Check if they implement ITrigger
- Support any ITrigger type, not just PopBumper

### PostRoundUI
Added to BumperType enum:
- `Rollover`
- `DropTarget`

Both components are now available for selection in the post-round UI.

## Technical Notes

- Both components properly implement the ITrigger interface
- Both inherit from Area2D (not RigidBody2D like PopBumper)
- Both support the chain trigger system via Trigger() and TriggerSingle() methods
- DropTarget uses careful closure handling in event subscriptions to avoid bugs
- Components are compatible with the existing passive system (ChainTriggerPassive)
