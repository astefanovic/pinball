using Godot;
using System;

public partial class Paddle : StaticBody2D
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
        restAngle = Rotation;
        lastRotation = Rotation;
        if (InputAction == "paddle_left")
            activeAngle = restAngle - Mathf.DegToRad(45); // Left paddle rotates counterclockwise
        else
            activeAngle = restAngle + Mathf.DegToRad(45); // Right paddle rotates clockwise

        // Create an Area2D for collision detection
        var area = new Area2D();
        AddChild(area);
        
        // Copy the collision shape to the area
        var shape = GetNode<CollisionShape2D>("CollisionShape2D");
        var areaShape = new CollisionShape2D();
        areaShape.Shape = shape.Shape;
        areaShape.Position = shape.Position;
        areaShape.Rotation = shape.Rotation;
        area.AddChild(areaShape);
        
        // Connect area signals
        area.BodyEntered += (body) => OnBodyEntered(body);
        
        GD.Print($"[Paddle {Name}] Initialized with collision area");
    }

    public void OnBodyEntered(Node body)
    {
        if (body is RigidBody2D rb && body is Node2D node2D && body.Name == "Pinball")
        {
            GD.Print($"[Paddle] Pre-collision - Ball Position: {node2D.GlobalPosition}, Velocity: {rb.LinearVelocity}, Paddle Rotation: {Mathf.RadToDeg(Rotation):F2}°");
            
            // Calculate paddle's true angular speed in radians per second
            float physicsDelta = (float)GetPhysicsProcessDeltaTime();
            float angularSpeedRadPerSec = 0f;
            if (physicsDelta > 0) // Avoid division by zero if physicsDelta is somehow zero
            {
                angularSpeedRadPerSec = (Rotation - lastRotation) / physicsDelta;
            }
            GD.Print($"[Paddle {Name}] Angular Speed: {Mathf.RadToDeg(angularSpeedRadPerSec):F2}°/sec (Delta: {physicsDelta:F4}s, AngleChange: {Mathf.RadToDeg(Rotation - lastRotation):F2}°)");

            // Calculate paddle's tip velocity (at collision point) using true angular speed
            float paddleLength = 600.0f; // Half-length of paddle based on scene
            Vector2 tipVelocityVec = new Vector2( // This is a velocity vector (speed * direction)
                -Mathf.Sin(Rotation) * paddleLength * angularSpeedRadPerSec,
                Mathf.Cos(Rotation) * paddleLength * angularSpeedRadPerSec
            );
            GD.Print($"[Paddle {Name}] Tip Velocity Vector: {tipVelocityVec}");

            // Calculate paddle direction vector based on current angle
            Vector2 paddleDir = new Vector2(Mathf.Cos(Rotation), Mathf.Sin(Rotation));
            GD.Print($"[Paddle {Name}] Paddle Direction Vector: {paddleDir}");
            
            // Calculate relative position of ball to paddle center
            Vector2 relativePos = node2D.GlobalPosition - GlobalPosition;
            float hitPosition = relativePos.Length();
            Vector2 relativePosNorm = relativePos.Normalized();
            GD.Print($"[Paddle {Name}] Hit Position: {hitPosition:F2}, Relative Direction: {relativePosNorm}");
            
            // Calculate velocity components
            Vector2 currentVel = rb.LinearVelocity;
            float speed = currentVel.Length();
            GD.Print($"[Paddle {Name}] Current Ball Speed: {speed:F2}");
            
            // Calculate impact point velocity (scaled by distance from pivot)
            float hitFraction = Mathf.Clamp(hitPosition / paddleLength, 0f, 1f);
            Vector2 impactVelocity = tipVelocityVec * hitFraction; // This is impact point's velocity vector
            GD.Print($"[Paddle {Name}] Impact Velocity: {impactVelocity}, Hit Fraction: {hitFraction:F2}");
            
            // Calculate paddle angular speed factor using true angular speed
            // The constant 8.333f is derived from the original 500f (500f / 60fps approx 8.333f).
            // This value might need further tuning for desired responsiveness.
            float angularSpeedFactor = Mathf.Abs(angularSpeedRadPerSec) * 8.333f; 
            
            // Calculate reflection direction considering paddle motion
            Vector2 motionInfluence = impactVelocity * 1.5f; // Increase impact of paddle motion. impactVelocity is now a true velocity.
            Vector2 baseDirection = (paddleDir + relativePosNorm).Normalized();
            Vector2 impulseDir = (baseDirection + motionInfluence.Normalized()).Normalized();
            GD.Print($"[Paddle {Name}] Base Direction: {baseDirection}, Motion Influence: {motionInfluence}, Angular Speed: {angularSpeedFactor:F2}");
            
            // Calculate impulse magnitude with speed limiting
            float baseImpulse = Input.IsActionPressed(InputAction) ? 7000f : 3000f; // Reduced base impulse
            
            // Add angular speed bonus
            baseImpulse += angularSpeedFactor;
            
            // Scale impulse based on hit position and angular speed
            float positionScale = 0.5f + hitFraction; // More force near the tip
            float movementScale = 1f + (angularSpeedFactor / 100f); // Additional scaling from paddle movement
            baseImpulse *= positionScale * movementScale;
            
            // Reduce impulse when ball is already moving fast
            float maxSpeed = 8000f;
            float speedFactor = Mathf.Max(0f, 1f - (speed / maxSpeed));
            baseImpulse *= speedFactor;
            
            // Calculate final impulse
            float finalImpulse = baseImpulse;
            
            // Cap the resulting velocity
            Vector2 expectedVelocity = currentVel + (impulseDir * finalImpulse);
            if (expectedVelocity.Length() > maxSpeed)
            {
                expectedVelocity = expectedVelocity.Normalized() * maxSpeed;
                finalImpulse = (expectedVelocity - currentVel).Length();
                impulseDir = (expectedVelocity - currentVel).Normalized();
            }
            GD.Print($"[Paddle] Base Impulse: {baseImpulse}, Final Impulse: {finalImpulse:F2}, IsFlipping: {Input.IsActionPressed(InputAction)}");
            
            // Apply impulse and optionally reset extreme velocities
            Vector2 appliedImpulse = impulseDir * finalImpulse;
            if (currentVel.Length() > maxSpeed)
            {
                // If current velocity is already too high, reset it
                rb.LinearVelocity = currentVel.Normalized() * maxSpeed;
                GD.Print($"[Paddle] Clamped excessive velocity: {rb.LinearVelocity}");
            }
            rb.ApplyImpulse(appliedImpulse);
            GD.Print($"[Paddle] Speed Factor: {speedFactor:F2}, Applied Impulse: {appliedImpulse}");
            
            // Add a timer to check post-collision state
            var timer = new Timer();
            AddChild(timer);
            timer.WaitTime = 0.02;
            timer.OneShot = true;
            timer.Timeout += () =>
            {
                GD.Print($"[Paddle] Post-collision - Ball Position: {node2D.GlobalPosition}, Velocity: {rb.LinearVelocity}");
                timer.QueueFree();
            };
            timer.Start();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float targetAngle = Input.IsActionPressed(InputAction) ? activeAngle : restAngle;
        bool isFlipping = Input.IsActionPressed(InputAction);
        
        // Calculate angular velocity
        float angularDelta = Mathf.Abs(Rotation - lastRotation);
        if (angularDelta > 0.01f || isFlipping)
        {
            GD.Print($"[Paddle {Name}] Rotation: {Mathf.RadToDeg(Rotation):F2}°, " +
                     $"Angular Delta: {Mathf.RadToDeg(angularDelta):F2}°, " +
                     $"IsFlipping: {isFlipping}");
        }
        
        lastRotation = Rotation;
        // Scale interpolation factor by delta for frame-rate independent rotation speed
        // The constant 24.0f is derived from the original 0.4f (0.4f * 60fps = 24.0f).
        // This value might need further tuning for desired responsiveness.
        // Mathf.Min is used to ensure the factor doesn't exceed 1.0 if delta is unusually large.
        Rotation = Mathf.Lerp(Rotation, targetAngle, Mathf.Min(24.0f * (float)delta, 1.0f)); // Smooth rotation
        
        // Debug print once to verify paddle is processing
        if (!debugPrinted)
        {
            GD.Print($"[Paddle {Name}] Initialized - RestAngle: {Mathf.RadToDeg(restAngle):F2}°, " +
                     $"ActiveAngle: {Mathf.RadToDeg(activeAngle):F2}°, " +
                     $"InputAction: {InputAction}");
            debugPrinted = true;
        }
    }
}
