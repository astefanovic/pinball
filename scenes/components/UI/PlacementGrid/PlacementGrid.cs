using Godot;
using System;

public partial class PlacementGrid : Control
{
    [Export] public Color GridColor = new Color(1, 1, 1, 0.2f); // White with 20% opacity
    [Export] public Color HoverColor = new Color(0, 1, 0, 0.5f); // Green with 50% opacity for hover

    // When false, grid will ignore click input (used while choosing a bumper)
    private bool _acceptClicks = false;
    public bool AcceptClicks
    {
        get => _acceptClicks;
        set
        {
            _acceptClicks = value;
            // When not accepting clicks, set MouseFilter to Ignore so it doesn't block UI behind it.
            MouseFilter = _acceptClicks ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
        }
    }

    // Signal to emit when a grid cell is clicked
    [Signal]
    public delegate void GridCellClickedEventHandler(Vector2I cellCoords);

    // Track hover state and selected component type for multi-cell highlighting
    private Vector2I _hoveredCell = new Vector2I(-1, -1);
    private PackedScene _selectedBumperScene = null;

public override void _Draw()
{
    Vector2 cellSize = new Vector2(Size.X / GridManager.Instance.Columns, Size.Y / GridManager.Instance.Rows);

    // Draw vertical lines
    for (int i = 0; i <= GridManager.Instance.Columns; i++)
        {
            float x = i * cellSize.X;
            DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), GridColor);
        }

    // Draw horizontal lines
    for (int i = 0; i <= GridManager.Instance.Rows; i++)
        {
            float y = i * cellSize.Y;
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), GridColor);
        }

    // Draw hover highlighting if a cell is hovered and a bumper is selected
    if (_hoveredCell.X >= 0 && _hoveredCell.Y >= 0 && _selectedBumperScene != null)
    {
        DrawHoverHighlight(cellSize);
    }
}

private void DrawHoverHighlight(Vector2 cellSize)
{
    // Get the cells that would be occupied by the selected component
    Vector2I[] cellsToHighlight = GetCellsToHighlight(_hoveredCell);
    
    // Draw highlight for each cell
    foreach (var cell in cellsToHighlight)
    {
        if (cell.X >= 0 && cell.X < GridManager.Instance.Columns && 
            cell.Y >= 0 && cell.Y < GridManager.Instance.Rows)
        {
            Vector2 cellPos = new Vector2(cell.X * cellSize.X, cell.Y * cellSize.Y);
            
            // Check if this cell is occupied
            bool isOccupied = GridManager.Instance.BumperGrid[cell.X, cell.Y] != null;
            Color highlightColor = isOccupied ? new Color(1, 0, 0, 0.8f) : HoverColor; // Red if occupied, green if free
            
            // Draw all four borders of the cell with slight insets to ensure visibility
            float borderWidth = 4.0f;
            float inset = 1.0f; // Small inset to ensure borders are visible
            
            // Top border
            DrawLine(new Vector2(cellPos.X + inset, cellPos.Y + inset), 
                    new Vector2(cellPos.X + cellSize.X - inset, cellPos.Y + inset), 
                    highlightColor, borderWidth);
            
            // Right border
            DrawLine(new Vector2(cellPos.X + cellSize.X - inset, cellPos.Y + inset), 
                    new Vector2(cellPos.X + cellSize.X - inset, cellPos.Y + cellSize.Y - inset), 
                    highlightColor, borderWidth);
            
            // Bottom border
            DrawLine(new Vector2(cellPos.X + cellSize.X - inset, cellPos.Y + cellSize.Y - inset), 
                    new Vector2(cellPos.X + inset, cellPos.Y + cellSize.Y - inset), 
                    highlightColor, borderWidth);
            
            // Left border
            DrawLine(new Vector2(cellPos.X + inset, cellPos.Y + cellSize.Y - inset), 
                    new Vector2(cellPos.X + inset, cellPos.Y + inset), 
                    highlightColor, borderWidth);
        }
    }
}

private Vector2I[] GetCellsToHighlight(Vector2I baseCell)
{
    if (_selectedBumperScene == null)
        return new Vector2I[] { baseCell };
    
    // Try to instantiate the component to check if it's multi-cell
    try
    {
        Node2D testComponent = _selectedBumperScene.Instantiate<Node2D>();
        Vector2I[] result;
        
        if (testComponent is IMultiCell multiCell)
        {
            result = new Vector2I[multiCell.OccupiedCells.Length];
            for (int i = 0; i < multiCell.OccupiedCells.Length; i++)
            {
                result[i] = baseCell + multiCell.OccupiedCells[i];
            }
        }
        else
        {
            result = new Vector2I[] { baseCell };
        }
        
        testComponent.QueueFree();
        return result;
    }
    catch
    {
        return new Vector2I[] { baseCell };
    }
}

    public override void _Ready()
    {
        // Ensure the control redraws when its size changes (e.g., in editor)
        SetProcessInternal(true);
        SetProcessInput(true); // Enable input processing for this control
        // Initialize MouseFilter according to AcceptClicks
        MouseFilter = _acceptClicks ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore;
    }

public override void _GuiInput(InputEvent @event)
{
    if (!AcceptClicks) return;

    Vector2 cellSize = new Vector2(Size.X / GridManager.Instance.Columns, Size.Y / GridManager.Instance.Rows);

    if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
    {
        Vector2 localPos = mouseButton.Position;

        int col = (int)(localPos.X / cellSize.X);
        int row = (int)(localPos.Y / cellSize.Y);

        // Ensure coordinates are within bounds
        if (col >= 0 && col < GridManager.Instance.Columns && row >= 0 && row < GridManager.Instance.Rows)
        {
            EmitSignal(SignalName.GridCellClicked, new Vector2I(col, row));
        }
    }
    else if (@event is InputEventMouseMotion mouseMotion)
    {
        Vector2 localPos = mouseMotion.Position;

        int col = (int)(localPos.X / cellSize.X);
        int row = (int)(localPos.Y / cellSize.Y);

        Vector2I newHoveredCell = new Vector2I(col, row);
        
        // Only update and redraw if the hovered cell changed
        if (newHoveredCell != _hoveredCell)
        {
            _hoveredCell = newHoveredCell;
            QueueRedraw();
        }
    }
}

public void SetSelectedBumperScene(PackedScene bumperScene)
{
    _selectedBumperScene = bumperScene;
    QueueRedraw(); // Redraw to update hover highlighting
}

public void ClearSelectedBumperScene()
{
    _selectedBumperScene = null;
    _hoveredCell = new Vector2I(-1, -1);
    QueueRedraw();
}

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            QueueRedraw();
        }
    }
}
