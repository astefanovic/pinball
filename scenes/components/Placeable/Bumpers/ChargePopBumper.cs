using Godot;
using System;

public partial class ChargePopBumper : PopBumper
{
    [Export]
    public ChargeComponent ChargeComponent { get; set; } // Reference to the ChargeComponent

    public override void _Ready()
    {
        base._Ready();

        if (ChargeComponent == null)
        {
            GD.PrintErr("ChargePopBumper: ChargeComponent is not assigned. Please assign it in the editor.");
            return;
        }
    }
}
