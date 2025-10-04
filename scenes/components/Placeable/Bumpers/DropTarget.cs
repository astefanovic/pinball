using Godot;
using System;
using System.Collections.Generic;

public partial class DropTarget : RigidBody2D, ITrigger, IMultiCell
{
    [Export]
    public float BumperForce = 1500.0f; // Same force as PopBumper

    public Vector2I GridPosition { get; set; }

    // IMultiCell implementation - DropTarget spans 2 cells horizontally
    public Vector2I CellSize => new Vector2I(2, 1);
    public Vector2I[] OccupiedCells => new Vector2I[] { new Vector2I(0, 0), new Vector2I(1, 0) };

    private bool[] _targetsHit = new bool[3]; // Track which targets have been hit
    private Node2D[] _targetVisuals = new Node2D[3]; // Visual representation of each target
    private Area2D[] _targetAreas = new Area2D[3]; // Collision areas for each target
    private CollisionShape2D[] _mainCollisionShapes = new CollisionShape2D[3]; // Main physics collision shapes
    private bool[] _targetPendingDisable = new bool[3]; // Wait until ball exits before disabling
    private bool _allTargetsHit = false;
    private PhysicsMaterial _physicsMaterial;
    private HittableComponent _hittableComponent;

    public override void _Ready()
    {
        // Setup physics properties like PopBumper
        Freeze = true; // Makes the body static
        FreezeMode = RigidBody2D.FreezeModeEnum.Static; // Specifies the type of freeze
        
        ContactMonitor = true;
        MaxContactsReported = 4; // Consistent with Pinball

        // Create and set physics material with high bounce and low friction
        _physicsMaterial = new PhysicsMaterial
        {
            Bounce = 1.2f, // Pop bumpers are usually very bouncy
            Friction = 0.0f
        };
        PhysicsMaterialOverride = _physicsMaterial; // For RigidBody2D, this applies the material
        
        // Find the HittableComponent
        _hittableComponent = GetNode<HittableComponent>("Hittable");
        
        GD.Print($"DropTarget: Initializing with {GetChildren().Count} children");
        
        // Find the main collision shapes for physics
        _mainCollisionShapes[0] = GetNode<CollisionShape2D>("MainCollision1");
        _mainCollisionShapes[1] = GetNode<CollisionShape2D>("MainCollision2");
        _mainCollisionShapes[2] = GetNode<CollisionShape2D>("MainCollision3");
        
        // Find the three target areas (child Area2D nodes)
        int targetIndex = 0;
        foreach (Node child in GetChildren())
        {
            GD.Print($"DropTarget: Found child: {child.Name} (Type: {child.GetType().Name})");
            if (child is Area2D area && child.Name.ToString().StartsWith("Target"))
            {
                if (targetIndex < 3)
                {
                    _targetAreas[targetIndex] = area;
                    GD.Print($"DropTarget: Setting up target {targetIndex} ({area.Name})");
                    
                    // Capture the index in a local variable to avoid closure issues
                    int index = targetIndex;
                    area.BodyEntered += (body) => OnTargetBodyEntered(body, index);
                    area.BodyExited += (body) => OnTargetBodyExited(body, index);
                    GD.Print($"DropTarget: Connected BodyEntered signal for target {targetIndex}");
                    GD.Print($"DropTarget: Connected BodyExited signal for target {targetIndex}");
                    
                    // Find the visual child (Polygon2D or similar)
                    foreach (Node visualChild in area.GetChildren())
                    {
                        if (visualChild is Node2D visual && !(visualChild is CollisionShape2D))
                        {
                            _targetVisuals[targetIndex] = visual;
                            GD.Print($"DropTarget: Found visual for target {targetIndex}: {visual.Name}");
                            break;
                        }
                    }
                    
                    targetIndex++;
                }
            }
        }

        if (_hittableComponent == null)
        {
            GD.PrintErr("DropTarget: HittableComponent could not be found. Please ensure it exists as 'Hittable' node.");
        }
    }

    private void OnTargetBodyEntered(Node2D body, int targetIndex)
    {
        GD.Print($"DropTarget: Body entered target {targetIndex}: {body?.Name} (Type: {body?.GetType().Name})");
        
        if (body is Pinball pinball && !_targetsHit[targetIndex])
        {
            GD.Print($"DropTarget: Target {targetIndex} hit by pinball!");
            
            // Mark this target as hit
            _targetsHit[targetIndex] = true;
            // Arm deferred disable to run when the ball exits this target's area
            _targetPendingDisable[targetIndex] = true;
            GD.Print($"DropTarget: Armed disable on exit for target {targetIndex}");

            // Check if all targets are hit
            bool allHit = true;
            for (int i = 0; i < 3; i++)
            {
                if (!_targetsHit[i])
                {
                    allHit = false;
                    break;
                }
            }

            if (allHit && !_allTargetsHit)
            {
                _allTargetsHit = true;
                GD.Print("DropTarget: All targets hit! Triggering and resetting...");
                TriggerSingle();
                
                // Reset targets after a delay
                GetTree().CreateTimer(1.0).Timeout += ResetTargets;
            }
        }
        else if (_targetsHit[targetIndex])
        {
            GD.Print($"DropTarget: Target {targetIndex} already hit, ignoring");
        }
        else
        {
            GD.Print($"DropTarget: Body is not a pinball: {body?.GetType().Name}");
        }
    }

    private void OnTargetBodyExited(Node2D body, int targetIndex)
    {
        GD.Print($"DropTarget: Body exited target {targetIndex}: {body?.Name} (Type: {body?.GetType().Name})");
        if (body is Pinball && _targetPendingDisable[targetIndex])
        {
            _targetPendingDisable[targetIndex] = false;

            // Retract the target visually (move down or hide)
            if (_targetVisuals[targetIndex] != null)
            {
                _targetVisuals[targetIndex].Visible = false;
                GD.Print($"DropTarget: Hidden visual for target {targetIndex}");
            }

            // Disable the detection area (must be deferred because we're inside BodyExited signal)
            if (_targetAreas[targetIndex] != null)
            {
                _targetAreas[targetIndex].SetDeferred("monitoring", false);
                GD.Print($"DropTarget: [Deferred] Disabled monitoring for target {targetIndex}");
            }

            // Disable the physics collision for this segment (also deferred)
            if (_mainCollisionShapes[targetIndex] != null)
            {
                _mainCollisionShapes[targetIndex].SetDeferred("disabled", true);
                GD.Print($"DropTarget: [Deferred] Disabled main collision shape for target {targetIndex}");
            }
            else
            {
                GD.PrintErr($"DropTarget: No main collision shape found for target {targetIndex}");
            }
        }
    }

    private void ResetTargets()
    {
        GD.Print("DropTarget: Resetting all targets");
        
        // Reset all targets
        for (int i = 0; i < 3; i++)
        {
            _targetsHit[i] = false;
            _targetPendingDisable[i] = false;
            
            if (_targetVisuals[i] != null)
            {
                _targetVisuals[i].Visible = true;
                GD.Print($"DropTarget: Made target {i} visual visible");
            }
            
            if (_targetAreas[i] != null)
            {
                _targetAreas[i].Monitoring = true;
                GD.Print($"DropTarget: Re-enabled monitoring for target {i}");
            }
            
            // Re-enable the physics body collision
            if (_mainCollisionShapes[i] != null)
            {
                _mainCollisionShapes[i].Disabled = false;
                GD.Print($"DropTarget: Re-enabled main collision shape for target {i}");
            }
        }
        
        _allTargetsHit = false;
        GD.Print("DropTarget: Reset complete");
    }

    // Call this to trigger the drop target and all adjacent bumpers
    public void Trigger(HashSet<ITrigger> triggered = null)
    {
        if (triggered == null)
            triggered = new HashSet<ITrigger>();

        if (triggered.Contains(this))
            return;

        triggered.Add(this);

        // Register hit (null pinball, use GlobalPosition)
        _hittableComponent?.RegisterHit(null, GlobalPosition);

        // Find and trigger adjacent bumpers using the grid
        int x = GridPosition.X;
        int y = GridPosition.Y;

        // Check all 8 neighbors
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0)
                    continue;

                int nx = x + i;
                int ny = y + j;

                if (nx >= 0 && nx < GridManager.Instance.BumperGrid.GetLength(0) && ny >= 0 && ny < GridManager.Instance.BumperGrid.GetLength(1))
                {
                    GridManager.Instance.BumperGrid[nx, ny]?.Trigger(triggered);
                }
            }
        }
    }

    /// <summary>
    /// Trigger only local effects on this drop target (do not cascade to neighbors).
    /// Used when all 3 targets have been hit.
    /// </summary>
    public void TriggerSingle()
    {
        // Register hit (null pinball, use GlobalPosition) but do NOT trigger neighbors
        _hittableComponent?.RegisterHit(null, GlobalPosition);
    }
}
