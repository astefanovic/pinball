using Godot;
using System;

public partial class Main : Node
{
    private PackedScene pinballScene;
    private Pinball currentBall;

    public override void _Ready()
    {
        pinballScene = GD.Load<PackedScene>("res://scenes/objects/pinball.tscn");
        SpawnBall();
        GD.Print("Main: Ready called, spawning initial ball");
    }

    private void SpawnBall()
    {
        GD.Print("Main: SpawnBall called");
        if (currentBall != null && IsInstanceValid(currentBall))
        {
            GD.Print("Main: Removing old ball");
            currentBall.QueueFree();
        }
        currentBall = pinballScene.Instantiate<Pinball>();
        currentBall.Position = new Vector2(7600, 6500); // Set initial position
        AddChild(currentBall);
        currentBall.BallOut += OnBallOut;
        GD.Print("Main: New ball spawned");
    }

    private void OnBallOut()
    {
        GD.Print("Main: BallOut signal received");
        SpawnBall();
    }
}
