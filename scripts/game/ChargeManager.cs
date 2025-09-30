using Godot;
using System;

public static class ChargeManager
{
    public static int Charge { get; private set; } = 0;

    public static event Action<int> OnChargeChanged;
    public static event Action<int> OnChargeScored;

    public static void IncrementCharge(int amount)
    {
        Charge += amount;
        OnChargeChanged?.Invoke(Charge);
        OnChargeScored?.Invoke(amount);
        GD.Print($"ChargeManager: Charge incremented by {amount}, total: {Charge}");
    }

    public static void ResetCharge()
    {
        Charge = 0;
        OnChargeChanged?.Invoke(Charge);
        GD.Print("ChargeManager: Charge reset to 0");
    }
}
