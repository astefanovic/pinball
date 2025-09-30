using Godot;
using System;

public partial class GoldComponent : Node
{
    [Export]
    public int DefaultGoldIncrementAmount { get; set; } = 1; // Amount to increment gold by

    public override void _Ready()
    {
        // Avoid subscribing when used inside PostRoundUI previews to prevent duplicate logs.
        if (!IsInsideTree() || GetTree().Root == null) return;
        Node ancestor = this;
        while (ancestor != null)
        {
            if (ancestor.Name == "PostRoundUI")
                return;
            ancestor = ancestor.GetParent();
        }

        GoldManager.OnGoldScored += _OnGoldScored;
        GoldManager.OnGoldChanged += _OnGoldChanged;
    }

    public void IncrementGold()
    {
        GoldManager.IncrementGold(DefaultGoldIncrementAmount);
    }

    private void _OnGoldScored(int amount)
    {
        GD.Print($"GoldComponent: Gold scored by {amount}!");
    }

    private void _OnGoldChanged(int total)
    {
        GD.Print($"GoldComponent: Gold total is now {total}.");
    }
}
