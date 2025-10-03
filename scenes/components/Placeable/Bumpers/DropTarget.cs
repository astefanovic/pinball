using Godot;
using System;
using System.Collections.Generic;

public partial class DropTarget : Area2D, ITrigger
{
    [Export]
    public HittableComponent HittableComponent { get; set; }

    public Vector2I GridPosition { get; set; }

    private bool[] _targetsHit = new bool[3]; // Track which targets have been hit
    private Node2D[] _targetVisuals = new Node2D[3]; // Visual representation of each target
    private Area2D[] _targetAreas = new Area2D[3]; // Collision areas for each target
    private bool _allTargetsHit = false;

    public override void _Ready()
    {
        // Find the three target areas (child Area2D nodes)
        int targetIndex = 0;
        foreach (Node child in GetChildren())
        {
            if (child is Area2D area && child.Name.ToString().StartsWith("Target"))
            {
                if (targetIndex < 3)
                {
                    _targetAreas[targetIndex] = area;
                    
                    // Capture the index in a local variable to avoid closure issues
                    int index = targetIndex;
                    area.BodyEntered += (body) => OnTargetBodyEntered(body, index);
                    
                    // Find the visual child (Polygon2D or similar)
                    foreach (Node visualChild in area.GetChildren())
                    {
                        if (visualChild is Node2D visual && !(visualChild is CollisionShape2D))
                        {
                            _targetVisuals[targetIndex] = visual;
                            break;
                        }
                    }
                    
                    targetIndex++;
                }
            }
        }

        if (HittableComponent == null)
        {
            // Try to auto-find a HittableComponent as a child
            foreach (Node child in GetChildren())
            {
                if (child is HittableComponent hc)
                {
                    HittableComponent = hc;
                    GD.Print($"DropTarget: Auto-assigned HittableComponent from child '{child.Name}'.");
                    break;
                }
            }

            if (HittableComponent == null)
            {
                GD.PrintErr("DropTarget: HittableComponent is not assigned and could not be auto-resolved. Please assign it in the editor.");
            }
        }
    }

    private void OnTargetBodyEntered(Node2D body, int targetIndex)
    {
        if (body is Pinball pinball && !_targetsHit[targetIndex])
        {
            // Mark this target as hit
            _targetsHit[targetIndex] = true;
            
            // Retract the target visually (move down or hide)
            if (_targetVisuals[targetIndex] != null)
            {
                _targetVisuals[targetIndex].Visible = false;
            }
            
            // Disable the collision for this target
            if (_targetAreas[targetIndex] != null)
            {
                _targetAreas[targetIndex].SetDeferred("monitoring", false);
            }

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
                TriggerSingle();
                
                // Reset targets after a delay
                GetTree().CreateTimer(1.0).Timeout += ResetTargets;
            }
        }
    }

    private void ResetTargets()
    {
        // Reset all targets
        for (int i = 0; i < 3; i++)
        {
            _targetsHit[i] = false;
            
            if (_targetVisuals[i] != null)
            {
                _targetVisuals[i].Visible = true;
            }
            
            if (_targetAreas[i] != null)
            {
                _targetAreas[i].SetDeferred("monitoring", true);
            }
        }
        
        _allTargetsHit = false;
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
        HittableComponent?.RegisterHit(null, GlobalPosition);

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
        HittableComponent?.RegisterHit(null, GlobalPosition);
    }
}
