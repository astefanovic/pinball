using Godot;
using System;

public partial class Bumper : StaticBody2D
{
    [Export]
    public float BumperForce = 4000.0f;
    [Export]
    public int ScoreValue = 10;
    [Export]
    public ScoreParticle scoreParticle;

    private PhysicsMaterial _physicsMaterial;
    private Main mainNode; // To access score

    public override void _Ready()
    {
        // Create and set physics material with high bounce and low friction
        _physicsMaterial = new PhysicsMaterial
        {
            Bounce = 1.0f,
            Friction = 0.0f
        };
        PhysicsMaterialOverride = _physicsMaterial;
        
        // Get reference to Main node
        mainNode = GetTree().Root.GetNode<Main>("Main");
        if (mainNode == null)
        {
            GD.PrintErr("Bumper: Could not find Main node. Ensure your main scene root is named 'Main'.");
        }
    }

    public void OnBodyEntered(Node2D body)
    {
        if (body is Pinball pinball) // Check specifically for Pinball
        {
            GD.Print("Bumper: Pinball entered bumper!");
            // Calculate reflection vector
            Vector2 normal = (pinball.GlobalPosition - GlobalPosition).Normalized();
            
            // Apply force along the normal, regardless of approach angle
            pinball.ApplyImpulse(normal * BumperForce);

            // Add score
            if (mainNode != null)
            {
                mainNode.AddScore(ScoreValue);
            }

            // Trigger particle effect
            if (scoreParticle != null)
            {
                scoreParticle.Populate(ScoreValue);
            }
        }
    }
}
