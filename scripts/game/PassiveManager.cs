using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PassiveManager : Node
{
    public static PassiveManager Instance { get; private set; }
    
    private List<IPassive> _activePassives = new List<IPassive>();
    private List<IPassive> _availablePassives = new List<IPassive>();
    
    public IReadOnlyList<IPassive> ActivePassives => _activePassives.AsReadOnly();
    public IReadOnlyList<IPassive> AvailablePassives => _availablePassives.AsReadOnly();
    
    [Signal]
    public delegate void PassiveAcquiredEventHandler(string passiveName);
    
    [Signal]
    public delegate void PassiveRemovedEventHandler(string passiveName);
    
    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            GD.PrintErr("PassiveManager: Multiple instances detected! This should be a singleton.");
            QueueFree();
        }
    }
    
    public override void _ExitTree()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    public override void _Ready()
    {
        InitializeAvailablePassives();
    }
    
    private void InitializeAvailablePassives()
    {
        // Create instances of all available passive types
        var chainTriggerPassive = new ChainTriggerPassive();
        AddChild(chainTriggerPassive); // Add to scene tree for proper lifecycle
        _availablePassives.Add(chainTriggerPassive);
        
        GD.Print($"PassiveManager: Initialized with {_availablePassives.Count} available passives");
    }
    
    public void RegisterPassive(IPassive passive)
    {
        if (passive == null || _activePassives.Contains(passive))
            return;
            
        _activePassives.Add(passive);
        EmitSignal(SignalName.PassiveAcquired, passive.Name);
        GD.Print($"PassiveManager: Registered passive '{passive.Name}'");
    }
    
    public void UnregisterPassive(IPassive passive)
    {
        if (passive == null || !_activePassives.Contains(passive))
            return;
            
        _activePassives.Remove(passive);
        EmitSignal(SignalName.PassiveRemoved, passive.Name);
        GD.Print($"PassiveManager: Unregistered passive '{passive.Name}'");
    }
    
    public void AcquirePassive(IPassive passive)
    {
        if (passive == null)
        {
            GD.PrintErr("PassiveManager: Cannot acquire null passive");
            return;
        }
        
        if (_activePassives.Contains(passive))
        {
            GD.Print($"PassiveManager: Passive '{passive.Name}' is already active");
            return;
        }
        
        passive.OnAcquired();
        GD.Print($"PassiveManager: Acquired passive '{passive.Name}'");
    }
    
    public void RemovePassive(IPassive passive)
    {
        if (passive == null || !_activePassives.Contains(passive))
            return;
            
        passive.OnRemoved();
        GD.Print($"PassiveManager: Removed passive '{passive.Name}'");
    }
    
    public void ClearAllPassives()
    {
        var passivesToRemove = _activePassives.ToList();
        foreach (var passive in passivesToRemove)
        {
            RemovePassive(passive);
        }
    }
    
    /// <summary>
    /// Called when a pop bumper is hit - triggers chain reactions if the passive is active
    /// </summary>
    /// <param name="bumper">The bumper that was hit</param>
    public void OnPopBumperHit(PopBumper bumper)
    {
        // Check if chain trigger passive is active
        var chainTriggerPassive = _activePassives.OfType<ChainTriggerPassive>().FirstOrDefault();
        if (chainTriggerPassive != null)
        {
            chainTriggerPassive.TriggerChainReaction(bumper);
        }
    }
    
    /// <summary>
    /// Get 3 random passives for the post-round UI selection
    /// </summary>
    /// <returns>Array of 3 random available passives</returns>
    public IPassive[] GetRandomPassivesForSelection()
    {
        if (_availablePassives.Count == 0)
        {
            GD.PrintErr("PassiveManager: No available passives for selection");
            return new IPassive[0];
        }
        
        var rng = new Random();
        var shuffled = _availablePassives.OrderBy(x => rng.Next()).ToArray();
        
        // Return up to 3 passives, or all if there are fewer than 3
        int count = Math.Min(3, shuffled.Length);
        var result = new IPassive[count];
        Array.Copy(shuffled, result, count);
        
        return result;
    }
}