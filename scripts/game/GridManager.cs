using Godot;
using System;

public partial class GridManager : Node
{
    public static GridManager Instance { get; private set; }

    [Export] public int Rows = 4;
    [Export] public int Columns = 4;

    public ITrigger[,] BumperGrid { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        BumperGrid = new ITrigger[Rows, Columns];
    }

    /// <summary>
    /// Removes a component from the grid, handling both single-cell and multi-cell components
    /// </summary>
    public void RemoveComponent(ITrigger component)
    {
        if (component == null) return;

        // Search for and remove all instances of this component from the grid
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                if (BumperGrid[x, y] == component)
                {
                    BumperGrid[x, y] = null;
                }
            }
        }
    }

    /// <summary>
    /// Checks if a component can be placed at the given position, considering multi-cell components
    /// </summary>
    public bool CanPlaceComponent(Vector2I gridPosition, IMultiCell multiCell = null)
    {
        if (multiCell != null)
        {
            foreach (var cellOffset in multiCell.OccupiedCells)
            {
                int checkX = gridPosition.X + cellOffset.X;
                int checkY = gridPosition.Y + cellOffset.Y;
                
                // Check bounds
                if (checkX < 0 || checkX >= Columns || checkY < 0 || checkY >= Rows)
                    return false;
                
                // Check if cell is occupied
                if (BumperGrid[checkX, checkY] != null)
                    return false;
            }
            return true;
        }
        else
        {
            // Single-cell component
            return gridPosition.X >= 0 && gridPosition.X < Columns && 
                   gridPosition.Y >= 0 && gridPosition.Y < Rows && 
                   BumperGrid[gridPosition.X, gridPosition.Y] == null;
        }
    }
}
