using Godot;
using System;
using System.Collections.Generic;

public partial class ChainTriggerPassive : BasePassive
{
    public override void _Ready()
    {
        Name = "Chain Reaction";
        Description = "Pop bumpers trigger adjacent bumpers";
        IconScene = GD.Load<PackedScene>("res://scenes/components/Passives/ChainTriggerPassive.tscn");
    }
    
    public override void OnAcquired()
    {
        base.OnAcquired();
        // Register this passive with the PassiveManager
        PassiveManager.Instance?.RegisterPassive(this);
    }
    
    public override void OnRemoved()
    {
        base.OnRemoved();
        // Unregister this passive from the PassiveManager
        PassiveManager.Instance?.UnregisterPassive(this);
    }
    
    /// <summary>
    /// Called by PassiveManager when a pop bumper is hit to trigger chain reactions
    /// </summary>
    /// <param name="originBumper">The bumper that was originally hit</param>
    /// <param name="triggeredBumpers">Set of already triggered bumpers to prevent infinite loops</param>
    public void TriggerChainReaction(PopBumper originBumper, HashSet<ITrigger> triggeredBumpers = null)
    {
        if (originBumper == null || GridManager.Instance == null)
            return;
            
        if (triggeredBumpers == null)
            triggeredBumpers = new HashSet<ITrigger>();
            
        if (triggeredBumpers.Contains(originBumper))
            return;
            
        triggeredBumpers.Add(originBumper);
        
        // Get the grid position of the origin bumper
        int x = originBumper.GridPosition.X;
        int y = originBumper.GridPosition.Y;
        
        // Trigger only cardinal neighbors (no diagonals) and only regular PopBumpers.
        // Use TriggerSingle on adjacent PopBumpers so the effect does not propagate further.
        var orthogonalOffsets = new (int dx, int dy)[] { (1,0), (-1,0), (0,1), (0,-1) };
        foreach (var (dx, dy) in orthogonalOffsets)
        {
            int nx = x + dx;
            int ny = y + dy;

            if (nx >= 0 && nx < GridManager.Instance.BumperGrid.GetLength(0) && ny >= 0 && ny < GridManager.Instance.BumperGrid.GetLength(1))
            {
                var adjacent = GridManager.Instance.BumperGrid[nx, ny];
                if (adjacent is PopBumper pop && !triggeredBumpers.Contains(pop))
                {
                    // Mark it as triggered to avoid duplicates in this pass
                    triggeredBumpers.Add(pop);
                    // Trigger only the single bumper effect (no further propagation)
                    pop.TriggerSingle();
                }
            }
        }
    }
}