using Godot;
using System;

public partial class MultManager : Node
{
    public static event Action<int> OnMultChanged;
    public static event Action OnMultStarted;
    public static event Action OnMultReset;

    private static MultManager _instance;
    private static int _mult = 1;

    public static int Mult
    {
        get => _mult;
        private set
        {
            _mult = value;
            OnMultChanged?.Invoke(_mult);
        }
    }

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        _instance = this;
        Mult = 1;
    }

    public static void IncrementMult(int amount = 1)
    {
        if (_mult == 1)
        {
            OnMultStarted?.Invoke();
        }
        Mult += amount;
    }

    public static void Reset()
    {
        Mult = 1;
        OnMultReset?.Invoke();
    }
}
