using Godot;
using System;

public partial class BurnManager : Node
{
    public static event Action<int> OnScoreBurned;
    public static event Action OnBurnStarted;
    public static event Action OnBurnStopped;

    public static int BurnAmount { get; private set; } = 0;
    private static bool _isBurning = false;
    private Timer _burnTimer;

    // Static instance to ensure singleton access
    private static BurnManager _instance;

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;

        _instance = this; // Assign the current instance to the static field

        _burnTimer = new Timer();
        AddChild(_burnTimer);
        _burnTimer.WaitTime = 0.5;
        _burnTimer.OneShot = false;
        _burnTimer.Autostart = false;
        _burnTimer.Timeout += _OnBurnTimerTimeout;
        // Subscribe to RoundManager's ball-out event so we can stop burning when a round ends.
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.BallOut += StopBurn;
        }
        else
        {
            // Try to find a RoundManager node in the scene tree and subscribe if found.
            var rm = GetNodeOrNull<RoundManager>("/root/Root") ?? GetNodeOrNull<RoundManager>("RoundManager");
            if (rm != null)
                rm.BallOut += StopBurn;
        }
    }

    public static void IncrementBurn(int amount = 1)
    {
        if (!_isBurning)
        {
            _isBurning = true;
            _instance?._burnTimer.Start(); // Use the static instance to access the timer
            OnBurnStarted?.Invoke();
            GD.Print("Burn effect started!");
        }
        BurnAmount += amount;
    }

    public static void StopBurn()
    {
        if (_isBurning)
        {
            _isBurning = false;
            _instance?._burnTimer.Stop(); // Use the static instance to access the timer
            OnBurnStopped?.Invoke();
            GD.Print("Burn effect stopped!");
        }
    }

    private void _OnBurnTimerTimeout()
    {
        if (BurnAmount > 0)
        {
            OnScoreBurned?.Invoke(BurnAmount);
            GD.Print($"Score burned by {BurnAmount}!");
            BurnAmount--;
        }
        
        if (BurnAmount <= 0)
        {
            StopBurn(); 
        }
    }
}
