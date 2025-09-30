using Godot;
using System;

public partial class RoundManager : Node
{
    public static RoundManager Instance { get; private set; }

    // Event fired when a round-ending ball-out occurs.
    public event Action BallOut;

    [Export]
    public bool DebugContacts = true;

    private int _contactsThisSecond = 0;
    private float _accum = 0f;

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        Instance = this;
    }

    public void NotifyBallOut()
    {
        BallOut?.Invoke();
    }

    // Called by Pinball to report the number of contacts processed this physics step.
    public void ReportContactCount(int numContacts)
    {
        if (!DebugContacts) return;
        _contactsThisSecond += numContacts;
    }

    public override void _Process(double delta)
    {
        if (!DebugContacts) return;
        _accum += (float)delta;
        if (_accum >= 1.0f)
        {
            GD.Print($"[Debug] Contacts last second: {_contactsThisSecond}");
            _contactsThisSecond = 0;
            _accum = 0f;
        }
    }
}
