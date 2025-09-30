using Godot;
using System;

public partial class GridManager : Node
{
    public static GridManager Instance { get; private set; }

    [Export] public int Rows = 4;
    [Export] public int Columns = 4;

    public PopBumper[,] BumperGrid { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        BumperGrid = new PopBumper[Rows, Columns];
    }
}
