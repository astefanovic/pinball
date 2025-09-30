using Godot;
using System;

public partial class ChargeComponent : Node
{
    [Export]
    public int DefaultChargeIncrementAmount { get; set; } = 1; // Amount to increment charge by

    public override void _Ready()
    {
        ChargeManager.OnChargeScored += _OnChargeScored;
        ChargeManager.OnChargeChanged += _OnChargeChanged;
    }

    public void IncrementCharge()
    {
        ChargeManager.IncrementCharge(DefaultChargeIncrementAmount);
    }

    private void _OnChargeScored(int amount)
    {
        GD.Print($"ChargeComponent: Charge scored by {amount}!");
    }

    private void _OnChargeChanged(int total)
    {
        GD.Print($"ChargeComponent: Charge total is now {total}.");
    }
}
