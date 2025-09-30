using Godot;
using System;

public partial class Paddle : AnimatableBody2D
{
    [Export]
    public string InputAction = "";

    private float restAngle = 0f;
    private float activeAngle = 0f;
    private float lastRotation = 0f;
    private bool debugPrinted = false;

    public override void _Ready()
    {
        base._Ready();

        if (InputAction == "paddle_right")
        {
            RotationDegrees = -30.0f; 
        }
        else if (InputAction == "paddle_left")
        {
            RotationDegrees = 30.0f;
        }
        // Ensure Rotation is updated from RotationDegrees if changed.
        // This might be redundant if RotationDegrees directly updates Rotation, but good for safety.
        // Rotation = Mathf.DegToRad(RotationDegrees); 

        restAngle = Rotation; // restAngle is in radians
        lastRotation = Rotation;

        if (InputAction == "paddle_left")
        {
            activeAngle = restAngle - Mathf.DegToRad(45); // Target: -15 deg
        }
        else // paddle_right
        {
            // For right paddle, if restAngle is -30 deg (radians), we want to rotate +45 deg LOCALLY.
            // This should result in a visual clockwise rotation.
            activeAngle = restAngle + Mathf.DegToRad(45); // Target: +15 deg local
        }
        
        GD.Print($"[Paddle {Name}] Initialized. Initial RotationDegrees: {RotationDegrees:F2}. RestAngle (rad): {restAngle:F2}. ActiveAngle (rad): {activeAngle:F2}.");
    }

    // Helper function to smoothly interpolate angles, handling wraparound
    private float LerpAngle(float from, float to, float weight)
    {
        float difference = Mathf.Wrap(to - from, -Mathf.Pi, Mathf.Pi);
        return from + difference * weight;
    }

    public override void _PhysicsProcess(double delta)
    {
        bool isPressed = Input.IsActionPressed(InputAction);
        float targetAngleRad = isPressed ? activeAngle : restAngle; // This is in radians
        bool isFlipping = isPressed;
        
    // Calculate angular velocity (using radians)
    float angularDeltaRad = Mathf.Abs(Rotation - lastRotation); // Rotation is in radians
        /*if (angularDeltaRad > Mathf.DegToRad(0.1f) || isFlipping) // Check a small threshold in radians
        {
            GD.Print($"[Paddle {Name}] Rotation (deg): {RotationDegrees:F2}°, " +
                     $"Angular Delta (deg): {Mathf.RadToDeg(angularDeltaRad):F2}°, " +
                     $"TargetAngle (deg): {Mathf.RadToDeg(targetAngleRad):F2}°, " +
                     $"IsFlipping: {isFlipping}");
        }*/
        
    lastRotation = Rotation; // Store current rotation in radians

    // Precompute lerp weight for stability and to avoid allocating intermediate values repeatedly
    float weight = Mathf.Min(24.0f * (float)delta, 1.0f);
    Rotation = LerpAngle(Rotation, targetAngleRad, weight);
        
        // Debug print once to verify paddle is processing
        /*if (!debugPrinted)
        {
            GD.Print($"[Paddle {Name}] Initialized - RestAngle: {Mathf.RadToDeg(restAngle):F2}°, " +
                     $"ActiveAngle: {Mathf.RadToDeg(activeAngle):F2}°, " +
                     $"InputAction: {InputAction}");
            debugPrinted = true;
        }*/
    }
}
