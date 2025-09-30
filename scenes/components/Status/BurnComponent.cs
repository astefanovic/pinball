using Godot;
using System;

public partial class BurnComponent : Node
{
    [Export]
    public int BurnIncrementAmount { get; set; } = 1; // Amount to increment burn by

    public override void _Ready()
    {
        // Subscribe to BurnManager's static events
        BurnManager.OnScoreBurned += _OnScoreBurned;
        BurnManager.OnBurnStarted += _OnBurnStarted;
        BurnManager.OnBurnStopped += _OnBurnStopped;
    }

    public void IncrementBurn() // No longer takes an amount, uses its own export
    {
        BurnManager.IncrementBurn(BurnIncrementAmount);
    }

    // These methods are now event handlers for BurnManager events
    private void _OnScoreBurned(int amount)
    {
        // Handle visual updates or other BurnComponent specific logic here
        GD.Print($"BurnComponent: Score burned by {amount}!");
    }

    private void _OnBurnStarted()
    {
        GD.Print("BurnComponent: Burn effect started!");
    }

    private void _OnBurnStopped()
    {
        GD.Print("BurnComponent: Burn effect stopped!");
    }
}
