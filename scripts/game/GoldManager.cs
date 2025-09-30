using Godot;
using System;

public static class GoldManager
{
    public static int Gold { get; private set; } = 0;

    public static event Action<int> OnGoldChanged;
    public static event Action<int> OnGoldScored;

    public static void IncrementGold(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
        OnGoldScored?.Invoke(amount);
        GD.Print($"GoldManager: Gold incremented by {amount}, total: {Gold}");
    }

    public static void ResetGold()
    {
        Gold = 0;
        OnGoldChanged?.Invoke(Gold);
        GD.Print("GoldManager: Gold reset to 0");
    }
}
