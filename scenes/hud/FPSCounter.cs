using Godot;

public partial class FPSCounter : Control
{
    private Label _label;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
    }

    public override void _Process(double delta)
    {
        _label.Text = $"FPS: {(int)Engine.GetFramesPerSecond()}";
    }
}
