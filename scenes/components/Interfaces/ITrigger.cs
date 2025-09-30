using Godot;
using System.Collections.Generic;

public interface ITrigger
{
    // Passes a HashSet of ITrigger to avoid recursive triggering
    public void Trigger(HashSet<ITrigger> triggered = null);
    
    // Grid position for all triggerable components
    public Vector2I GridPosition { get; set; }
    
    // Global position for placement
    public Vector2 GlobalPosition { get; set; }
}
