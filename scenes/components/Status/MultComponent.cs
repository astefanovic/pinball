using Godot;

public partial class MultComponent : Node
{
    [Export]
    public int MultIncrementAmount { get; set; } = 1;

    public void IncrementMult()
    {
        MultManager.IncrementMult(MultIncrementAmount);
    }
}
