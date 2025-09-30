using Godot;
using System;

public partial class ScoreParticle : Node2D
{
    [Export]
    public GpuParticles2D Particle { get; set; }

    public void SetColor(Color color)
    {
        if (Particle != null && Particle.ProcessMaterial is ParticleProcessMaterial material)
        {
            // Duplicate the material so changing color here does not affect other instances
            var inst = material.Duplicate() as ParticleProcessMaterial;
            if (inst != null)
            {
                inst.Color = color;
                Particle.ProcessMaterial = inst;
            }
            else
            {
                // Fallback: set on original (best-effort)
                material.Color = color;
            }
        }
    }

    public void Populate(int score)
    {
        int particleCount = score / 10;
        if (particleCount < 1) particleCount = 0;

            GD.Print($"ScoreParticle: Emitting {particleCount} particles.");

            // Emit a single particle
            Particle.Amount = particleCount;
            Particle.Restart();
    }
}
