using Godot;
using System;
using System.Collections.Generic;

public partial class Rollover : Area2D, ITrigger
{
    [Export]
    public HittableComponent HittableComponent { get; set; }

    public Vector2I GridPosition { get; set; }

    public override void _Ready()
    {
        // Connect the body_entered signal
        BodyEntered += OnBodyEntered;

        if (HittableComponent == null)
        {
            // Try to auto-find a HittableComponent as a child
            foreach (Node child in GetChildren())
            {
                if (child is HittableComponent hc)
                {
                    HittableComponent = hc;
                    GD.Print($"Rollover: Auto-assigned HittableComponent from child '{child.Name}'.");
                    break;
                }
            }

            if (HittableComponent == null)
            {
                GD.PrintErr("Rollover: HittableComponent is not assigned and could not be auto-resolved. Please assign it in the editor.");
            }
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Pinball pinball)
        {
            // Trigger the rollover when the ball passes through
            TriggerSingle();
        }
    }

    // Call this to trigger the rollover and all adjacent bumpers
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
    /// Trigger only local effects on this rollover (do not cascade to neighbors).
    /// Used for normal ball pass-through triggering.
    /// </summary>
    public void TriggerSingle()
    {
        // Register hit (null pinball, use GlobalPosition) but do NOT trigger neighbors
        HittableComponent?.RegisterHit(null, GlobalPosition);
    }
}
