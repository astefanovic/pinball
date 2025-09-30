using Godot;
using System;

public partial class PlacementGrid : Control
{
    [Export] public Color GridColor = new Color(1, 1, 1, 0.2f); // White with 20% opacity

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

    if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
    {
        Vector2 localPos = mouseButton.Position;
        Vector2 cellSize = new Vector2(Size.X / GridManager.Instance.Columns, Size.Y / GridManager.Instance.Rows);

        int col = (int)(localPos.X / cellSize.X);
        int row = (int)(localPos.Y / cellSize.Y);

        // Ensure coordinates are within bounds
        if (col >= 0 && col < GridManager.Instance.Columns && row >= 0 && row < GridManager.Instance.Rows)
        {
            EmitSignal(SignalName.GridCellClicked, new Vector2I(col, row));
        }
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            QueueRedraw();
        }
    }
}
