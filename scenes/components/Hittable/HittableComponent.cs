using Godot;
using System;

public partial class HittableComponent : Node
{
    [Export]
    public ScoreParticle ScoreParticleComponent { get; set; }
    [Export]
    public Color DefaultParticleColor { get; set; } = new Color(1f,1f,1f,1f);
    [Export]
    public BurnComponent BurnComponent { get; set; } // Optional, for burnable objects
    [Export]
    public GoldComponent GoldComponent { get; set; } // Optional, for goldable objects
    [Export]
    public ChargeComponent ChargeComponent { get; set; } // Optional, for chargeable objects
    [Export]
    public MultComponent MultComponent { get; set; } // Optional, for mult bumpers
    [Export]
    public int ScoreValue { get; set; } = 0;

    public override void _Ready()
    {
        if (ScoreParticleComponent == null)
        {
            GD.PrintErr("Hittable: ScoreParticleComponent is not assigned. Please assign it in the editor.");
            return;
        }
        // Ensure ScoreParticleComponent is in the ScoreParticles group for signal connection
        if (!ScoreParticleComponent.IsInGroup("ScoreParticles"))
        {
            ScoreParticleComponent.AddToGroup("ScoreParticles");
        }
    }

    public void RegisterHit(Pinball pinball, Vector2 hitPosition)
    {
        // Handle special components first
        var particleData = new (object component, int amount, Color color, Action increment)[]
        {
            (BurnComponent, BurnComponent?.BurnIncrementAmount ?? 0, new Color(0.95f, 0.35f, 0.2f, 1f), () => BurnComponent?.IncrementBurn()),
            (GoldComponent, GoldComponent?.DefaultGoldIncrementAmount ?? 0, new Color(1.0f, 0.95f, 0.4f, 1f), () => GoldComponent?.IncrementGold()),
            (ChargeComponent, ChargeComponent?.DefaultChargeIncrementAmount ?? 0, new Color(0.5f, 0.95f, 1.0f, 1f), () => ChargeComponent?.IncrementCharge()),
            (MultComponent, MultComponent?.MultIncrementAmount ?? 0, new Color(0.7f, 0.7f, 1.0f, 1f), () => MultComponent?.IncrementMult())
        };

        foreach (var (component, amount, color, increment) in particleData)
        {
            if (component != null && amount > 0)
            {
                // Trigger visual effect, but don't populate score
                // Use the provided color for special components
                ScoreParticleComponent?.SetColor(color);
                ScoreParticleComponent?.Populate(10 * amount);
                increment();
            }
        }

        // Always add the base ScoreValue, if it's greater than 0
        if (ScoreValue > 0)
        {
            var main = GetTree().Root.GetNode<Main>("Main");
            if (main != null)
            {
                main.AddScore(ScoreValue, true); // Apply multiplier for bumper hits
            }
            // Trigger visual effect for the score using the bumper's default color
            ScoreParticleComponent?.SetColor(DefaultParticleColor);
            ScoreParticleComponent?.Populate(ScoreValue);
        }
    }
}
