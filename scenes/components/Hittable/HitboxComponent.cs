using Godot;
using System;

public partial class HitboxComponent : Area2D
{
    [Export]
    public CollisionShape2D CollisionShape2DNode { get; set; }
    [Export]
    public HittableComponent HittableComponent { get; set; }
    [Export]
    public float ImpulseForce { get; set; } = 0.0f;

    public override void _Ready()
    {
        if (CollisionShape2DNode == null)
        {
            GD.PrintErr("HitboxComponent: CollisionShape2DNode is not assigned. Please assign it in the editor.");
            return;
        }
        if (HittableComponent == null)
        {
            // Try to auto-find a HittableComponent on the parent node (common scene setups have Hitbox as a child of the bumper)
            var parent = GetParent();
            if (parent != null)
            {
                // Direct child with HittableComponent
                var candidate = parent.GetNodeOrNull<HittableComponent>("HittableComponent");
                if (candidate == null)
                {
                    // Search children of parent for a HittableComponent
                    foreach (Node child in parent.GetChildren())
                    {
                        if (child is HittableComponent hc)
                        {
                            candidate = hc;
                            break;
                        }
                    }
                }

                if (candidate != null)
                {
                    HittableComponent = candidate;
                    GD.Print($"HitboxComponent: Auto-assigned HittableComponent from parent '{parent.Name}'.");
                }
            }

            if (HittableComponent == null)
            {
                GD.PrintErr("HitboxComponent: HittableComponent is not assigned and could not be auto-resolved. Please assign it in the editor.");
                // Continue without returning so that other systems (like PassiveManager notifications) can still run,
                // but avoid null deref when RegisterHit is attempted.
            }
        }

        // Connect the body_entered signal
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Pinball pinball)
        {
            // Apply impulse
            pinball.ApplyImpulse((pinball.GlobalPosition - GlobalPosition).Normalized() * ImpulseForce);

            // For bumpers, only register a hit on the local hittable component so only the
            // bumper that was actually struck emits particles and awards score.
            // Register the hit on the local hittable component (if present)
            if (HittableComponent != null)
            {
                HittableComponent.RegisterHit(pinball, GlobalPosition); // Pass GlobalPosition for particle spawn
            }

            // Notify PassiveManager only if the direct parent is exactly a PopBumper (no subclasses).
            var parentNode = GetParent();
            if (parentNode != null && parentNode.GetType() == typeof(PopBumper) && PassiveManager.Instance != null)
            {
                PassiveManager.Instance.OnPopBumperHit((PopBumper)parentNode);
            }
        }
    }
}
