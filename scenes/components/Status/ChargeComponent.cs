using Godot;
using System;

public partial class ChargeComponent : Node
{
    [Export]
    public int DefaultChargeIncrementAmount { get; set; } = 1; // Amount to increment charge by

    public override void _Ready()
    {
        // Avoid subscribing when this component is used in UI preview scenes (PostRoundUI panels).
        // We only want runtime, placed bumpers to subscribe to static manager events.
        if (!IsInsideTree() || GetTree().Root == null) return;
        // If this node is inside the PostRoundUI preview (ancestor path contains PostRoundUI), don't subscribe
        Node ancestor = this;
        while (ancestor != null)
        {
            if (ancestor.Name == "PostRoundUI")
                return;
            ancestor = ancestor.GetParent();
        }

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
