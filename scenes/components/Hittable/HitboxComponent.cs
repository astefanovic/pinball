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
            GD.PrintErr("HitboxComponent: HittableComponent is not assigned. Please assign it in the editor.");
            return;
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
            if (HittableComponent != null)
            {
                HittableComponent.RegisterHit(pinball, GlobalPosition); // Pass GlobalPosition for particle spawn
            }
            
            // Notify PassiveManager if this is a PopBumper hit by a pinball
            // Only notify the PassiveManager if the direct parent is exactly a PopBumper
            var parentBumperExact = GetParent();
            if (parentBumperExact != null && parentBumperExact.GetType() == typeof(PopBumper) && PassiveManager.Instance != null)
            {
                PassiveManager.Instance.OnPopBumperHit((PopBumper)parentBumperExact);
            }
            
            else
            {
                GD.PrintErr("HitboxComponent: HittableComponent is null, cannot register hit.");
            }
        }
    }
}
