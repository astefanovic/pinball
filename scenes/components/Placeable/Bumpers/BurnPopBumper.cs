using Godot;
using System;

public partial class BurnPopBumper : PopBumper
{
    [Export]
    public BurnComponent BurnComponent { get; set; } // Reference to the BurnComponent

    public override void _Ready()
    {
        base._Ready();

        if (BurnComponent == null)
        {
            GD.PrintErr("BurnPopBumper: BurnComponent is not assigned. Please assign it in the editor.");
            return;
        }
    }
}
