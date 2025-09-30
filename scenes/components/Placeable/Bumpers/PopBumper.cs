using Godot;
using System;
using System.Collections.Generic;

public partial class PopBumper : RigidBody2D, ITrigger
{
    [Export]
    public float BumperForce = 1500.0f; // Smaller impulse
    [Export]
    public HitboxComponent HitboxComponent { get; set; }

    public Vector2I GridPosition { get; set; }

    private PhysicsMaterial _physicsMaterial;

    public override void _Ready()
    {
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

        if (HitboxComponent == null)
        {
            GD.PrintErr("PopBumper: HitboxComponent is not assigned. Please assign it in the editor.");
            return;
        }
        if (HitboxComponent.HittableComponent == null)
        {
            GD.PrintErr("PopBumper: HittableComponent within HitboxComponent is not assigned. Please assign it in the editor.");
            return;
        }
    }

    // Call this to trigger the bumper and all adjacent bumpers
    public void Trigger(HashSet<ITrigger> triggered = null)
    {
        if (triggered == null)
            triggered = new HashSet<ITrigger>();

        if (triggered.Contains(this))
            return;

        triggered.Add(this);

        // Register hit (null pinball, use GlobalPosition)
        HitboxComponent?.HittableComponent?.RegisterHit(null, GlobalPosition);

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
    /// Trigger only local effects on this bumper (do not cascade to neighbors).
    /// Used by passives that want to trigger adjacent bumpers without further propagation.
    /// </summary>
    public void TriggerSingle()
    {
        // Register hit (null pinball, use GlobalPosition) but do NOT trigger neighbors
        HitboxComponent?.HittableComponent?.RegisterHit(null, GlobalPosition);
    }
}
