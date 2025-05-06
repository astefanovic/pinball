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
        lastFrameVelocity = LinearVelocity;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Log significant velocity changes (potential collisions)
        Vector2 velocityDelta = LinearVelocity - lastFrameVelocity;
        if (velocityDelta.Length() > 100) // Only log significant changes
        {
            GD.Print($"[Ball] Velocity change detected - Previous: {lastFrameVelocity}, Current: {LinearVelocity}, Delta: {velocityDelta}");
        }
        lastFrameVelocity = LinearVelocity;

        if (Input.IsActionJustPressed("launch_ball"))
        {
            GD.Print($"[Ball] Launch input detected - Position: {Position}, LinearVelocity: {LinearVelocity}, Sleeping: {Sleeping}");
            Sleeping = false;
            ApplyCentralImpulse(new Vector2(0, -800));
        }

        // Emit signal if ball falls below the bottom of the screen
        if (Position.Y > 800)
        {
            EmitSignal("BallOut");
        }
    }

    public void OnBodyEntered(Node2D body)
    {
        GD.Print($"[Ball] Collision with {body.Name} - Position: {Position}, Velocity: {LinearVelocity}");
    }
}
