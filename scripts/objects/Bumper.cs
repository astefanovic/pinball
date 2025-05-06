using Godot;
using System;

public partial class Bumper : StaticBody2D
{
    [Export]
    public float BumperForce = 400.0f;

    private PhysicsMaterial _physicsMaterial;

    public override void _Ready()
    {
        // Create and set physics material with high bounce and low friction
        _physicsMaterial = new PhysicsMaterial
        {
            Bounce = 1.0f,
            Friction = 0.0f
        };
        PhysicsMaterialOverride = _physicsMaterial;
    }

    public void OnBodyEntered(Node2D body)
    {
        if (body is RigidBody2D rigidBody)
        {
            // Calculate reflection vector
            Vector2 normal = (rigidBody.GlobalPosition - GlobalPosition).Normalized();
            
            // Apply force along the normal, regardless of approach angle
            rigidBody.ApplyImpulse(normal * BumperForce);
        }
    }
}
