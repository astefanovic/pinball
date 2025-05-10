using Godot;
using System;

public partial class Pinball : RigidBody2D
{
    [Signal]
    public delegate void BallOutEventHandler();

    private Vector2 startPosition;
    private Vector2 lastFrameVelocity;

    public override void _Ready()
    {
        startPosition = Position;
        ContactMonitor = true;
        MaxContactsReported = 4;
        GravityScale = 10.0f; // Apply 10x gravity to this body
        lastFrameVelocity = LinearVelocity;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Log significant velocity changes (potential collisions)
        Vector2 velocityDelta = LinearVelocity - lastFrameVelocity;
        if (velocityDelta.Length() > 1000) // Only log significant changes
        {
            GD.Print($"[Ball] Velocity change detected - Previous: {lastFrameVelocity}, Current: {LinearVelocity}, Delta: {velocityDelta}");
        }
        lastFrameVelocity = LinearVelocity;

        if (Input.IsActionJustPressed("launch_ball"))
        {
            GD.Print($"[Ball] Launch input detected - Position: {Position}, LinearVelocity: {LinearVelocity}, Sleeping: {Sleeping}");
            Sleeping = false;
            ApplyCentralImpulse(new Vector2(0, -8000));
        }

        // Emit signal if ball falls below the bottom of the screen
        if (Position.Y > 8000)
        {
            EmitSignal("BallOut");
        }
    }

    public void OnBodyEntered(Node2D body)
    {
        GD.Print($"[Ball] Collision with {body.Name} - Position: {Position}, Velocity: {LinearVelocity}");
    }

    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
        base._IntegrateForces(state); // Call base method if it has any logic (good practice)

        for (int i = 0; i < state.GetContactCount(); i++)
        {
            var collider = state.GetContactColliderObject(i); // This is a GodotObject
            Node2D colliderNode2D = collider as Node2D;

            if (colliderNode2D != null && (colliderNode2D.Name.ToString().StartsWith("PaddleLeft") || colliderNode2D.Name.ToString().StartsWith("PaddleRight")))
            {
                GD.Print($"[Ball._IntegrateForces] Contact with {colliderNode2D.Name}:");
                GD.Print($"  Pinball Velocity (at start of integrate_forces): {state.LinearVelocity}");
                GD.Print($"  Contact Index: {i}");
                GD.Print($"  Contact Local Normal (on Pinball): {state.GetContactLocalNormal(i)}");
                GD.Print($"  Contact Local Position (on Pinball): {state.GetContactLocalPosition(i)}");
                // To get world position of contact on Pinball: Pinball's GlobalTransform * local_contact_pos
                GD.Print($"  Contact World Position (on Pinball surface approx): {GlobalTransform * state.GetContactLocalPosition(i)}");
                
                // To get world position of contact on Collider: Collider's GlobalTransform * contact_collider_pos
                // state.GetContactColliderPosition(i) is the contact point in the collider's local space.
                GD.Print($"  Contact Collider Local Position (on Paddle): {state.GetContactColliderPosition(i)}");
                GD.Print($"  Contact World Position (on Paddle surface approx): {colliderNode2D.GlobalTransform * state.GetContactColliderPosition(i)}");
                
                GD.Print($"  Contact Collider Velocity at Contact Point: {state.GetContactColliderVelocityAtPosition(i)}");
                GD.Print($"  Contact Impulse Applied: {state.GetContactImpulse(i)}"); // This is the impulse applied by the solver in this step.
            }
        }
    }
}
