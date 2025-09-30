using Godot;
using System;

public abstract partial class BasePassive : Node, IPassive
{
    [Export]
    public new string Name { get; protected set; } = "Unknown Passive";
    
    [Export]
    public string Description { get; protected set; } = "A passive ability";
    
    [Export]
    public PackedScene IconScene { get; protected set; }
    
    public virtual void OnAcquired()
    {
        GD.Print($"Passive acquired: {Name}");
    }
    
    public virtual void OnRemoved()
    {
        GD.Print($"Passive removed: {Name}");
    }
}