using Godot;
using System;

public partial class GoldPopBumper : PopBumper
{
    [Export]
    public GoldComponent GoldComponent { get; set; } // Reference to the GoldComponent

    public override void _Ready()
    {
        base._Ready();

        if (GoldComponent == null)
        {
            GD.PrintErr("GoldPopBumper: GoldComponent is not assigned. Please assign it in the editor.");
            return;
        }
    }
}
