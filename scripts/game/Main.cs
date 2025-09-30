using Godot;
using System;

public partial class Main : Node
{
    private PackedScene pinballScene;
    private Pinball currentBall;
    private Label scoreLabel;
    private Label burnAmountLabel;
    private Label goldAmountLabel;
    private Label chargeAmountLabel;
    private Label multAmountLabel;
    private Label targetScoreLabel;
    private Label failLabel;
    private float score;
    private int currentRound = 1;
    private int targetScore = 0;
    private bool roundFailed = false;
    private bool isBallOut = false;
    private const float SCORE_INCREMENT_PER_SECOND = 10.0f;
    private PackedScene postRoundUIScene;
    private PostRoundUI postRoundUI;
    private PlacementGrid placementGrid;
    private PackedScene popBumperScene;
    private PackedScene burnPopBumperScene;
    private PackedScene goldPopBumperScene;
    private PackedScene chargePopBumperScene;
    private PackedScene multBumperScene;

public override void _Ready()
{
    pinballScene = GD.Load<PackedScene>("res://scenes/objects/pinball.tscn");
    postRoundUIScene = GD.Load<PackedScene>("res://scenes/components/UI/PostRoundUI.tscn");
    popBumperScene = GD.Load<PackedScene>("res://scenes/components/Placeable/Bumpers/PopBumper.tscn"); // Preload PopBumper scene
    burnPopBumperScene = GD.Load<PackedScene>("res://scenes/components/Placeable/Bumpers/BurnPopBumper.tscn"); // Preload BurnPopBumper scene
    goldPopBumperScene = GD.Load<PackedScene>("res://scenes/components/Placeable/Bumpers/GoldPopBumper.tscn"); // Preload GoldPopBumper scene
    multBumperScene = GD.Load<PackedScene>("res://scenes/components/Placeable/Bumpers/MultBumper.tscn");
    scoreLabel = GetNode<Label>("ScoreLabel");
    burnAmountLabel = GetNode<Label>("BurnAmountLabel"); // Get the new label
    goldAmountLabel = GetNode<Label>("GoldAmountLabel");
    multAmountLabel = GetNode<Label>("MultAmountLabel");
    placementGrid = GetNode<PlacementGrid>("PlacementGrid");
    chargeAmountLabel = GetNode<Label>("ChargeAmountLabel");
    targetScoreLabel = GetNode<Label>("TargetScoreLabel");
    failLabel = GetNode<Label>("FailLabel");
    chargePopBumperScene = GD.Load<PackedScene>("res://scenes/components/Placeable/Bumpers/ChargePopBumper.tscn");
    ResetScore();
    GD.Print("Main: Ready called.");

    postRoundUI = postRoundUIScene.Instantiate<PostRoundUI>();
    // Create a dedicated UI root so Control nodes get proper layout and sizing.
    // We add a CanvasLayer with a top-level Control named "UIRoot" and parent UI there.
    var uiLayer = new CanvasLayer();
    uiLayer.Name = "UILayer";
    AddChild(uiLayer);

    var uiRoot = new Control();
    uiRoot.Name = "UIRoot";
    uiRoot.AnchorLeft = 0.0f;
    uiRoot.AnchorTop = 0.0f;
    uiRoot.AnchorRight = 1.0f;
    uiRoot.AnchorBottom = 1.0f;
    uiLayer.AddChild(uiRoot);

    // Parent PostRoundUI and PlacementGrid to the UI root so they participate in UI layout
    uiRoot.AddChild(postRoundUI);

    if (placementGrid != null)
    {
        var parent = placementGrid.GetParent();
        if (parent != null && parent != uiRoot)
        {
            parent.RemoveChild(placementGrid);
        }
        uiRoot.AddChild(placementGrid);
    }

    postRoundUI.SetPlacementGrid(placementGrid); // Pass the placementGrid instance
    // Subscribe to the strongly-typed C# event on the PostRoundUI
    postRoundUI.OnBumperSelectedEvent += OnBumperSelected;
    postRoundUI.OnPassiveSelectedEvent += OnPassiveSelected;
    
    // Create and add PassiveManager
    // Ensure a RoundManager exists early so other managers can subscribe to its events
    if (RoundManager.Instance == null)
    {
        var existingRm = GetNodeOrNull<RoundManager>("/root/Root") ?? GetNodeOrNull<RoundManager>("RoundManager");
        if (existingRm == null)
        {
            var roundManager = new RoundManager();
            AddChild(roundManager);
        }
    }

    var passiveManager = new PassiveManager();
    AddChild(passiveManager);
    BurnManager.OnScoreBurned += OnBurnScore;
    GoldManager.OnGoldChanged += OnGoldChanged;
    ChargeManager.OnChargeChanged += OnChargeChanged;
    MultManager.OnMultChanged += OnMultChanged;

    // Start the game immediately with a ball in play
    SpawnBall();
    postRoundUI.Hide();
    placementGrid.Hide();
}

    public override void _PhysicsProcess(double delta)
    {
        if (currentBall != null && IsInstanceValid(currentBall) && currentBall.IsInPlay()) // Assuming IsInPlay() will be added to Pinball.cs
        {
            score += SCORE_INCREMENT_PER_SECOND * (float)delta; // Accumulate as float
            UpdateScoreLabel();
        }
    }

    private void ConnectScoreParticleSignals()
    {
        // This method is no longer needed as score is handled by HittableComponent
    }

    private void OnScoreParticleScoreAdded(int scoreValue)
    {
        // This method is no longer needed as score is handled by HittableComponent
    }

private void UpdateScoreLabel()
{
    scoreLabel.Text = $"Score: {Mathf.RoundToInt(score)}"; // Display rounded integer
}

private void UpdateTargetScoreLabel()
{
    targetScoreLabel.Text = $"Target: {targetScore}";
}

private void UpdateFailLabel()
{
    failLabel.Visible = roundFailed;
    failLabel.Text = roundFailed ? "FAIL" : "";
}

    private void UpdateBurnAmountLabel()
    {
        burnAmountLabel.Text = $"Burn: {Mathf.RoundToInt(BurnManager.BurnAmount)}"; // Display rounded integer
    }

    private void UpdateGoldAmountLabel()
    {
        goldAmountLabel.Text = $"Gold: {GoldManager.Gold}";
    }

    private void UpdateChargeAmountLabel()
    {
        chargeAmountLabel.Text = $"Charge: {ChargeManager.Charge}";
    }

    private void UpdateMultAmountLabel()
    {
        multAmountLabel.Text = $"Mult: {MultManager.Mult}x";
    }

    private void OnGoldChanged(int gold)
    {
        UpdateGoldAmountLabel();
    }

    private void OnChargeChanged(int charge)
    {
        UpdateChargeAmountLabel();
    }

    private void OnMultChanged(int mult)
    {
        UpdateMultAmountLabel();
    }

    public void AddScore(int amount, bool applyMultiplier = false)
    {
        if (applyMultiplier)
            score += amount * MultManager.Mult;
        else
            score += amount;
        UpdateScoreLabel();
        GD.Print($"Main: Added {amount}{(applyMultiplier ? $" x{MultManager.Mult}" : "")} to score. New score: {Mathf.RoundToInt(score)}");
    }

private void ResetScore()
{
    score = 0;
    MultManager.Reset();
    // BurnManager.BurnAmount is managed by BurnManager itself, no need to reset here directly
    UpdateScoreLabel();
    UpdateBurnAmountLabel(); // Update burn amount label
    UpdateGoldAmountLabel(); // Update gold amount label
    UpdateChargeAmountLabel(); // Update charge amount label
    UpdateMultAmountLabel();
    // Set target score for the round
    targetScore = 200 * currentRound + 100 * (currentRound - 1);
    UpdateTargetScoreLabel();
    roundFailed = false;
    UpdateFailLabel();
    GD.Print($"Main: Score reset. Round {currentRound}, Target {targetScore}");
}

    private void SpawnBall()
    {
        isBallOut = false;
        GD.Print("Main: SpawnBall called");
        if (currentBall != null && IsInstanceValid(currentBall))
        {
            GD.Print("Main: Removing old ball");
            // Ensure we stop listening to BallOut from the old ball before freeing it to avoid stale callbacks.
            try
            {
                currentBall.BallOut -= OnBallOut;
            }
            catch {}
            currentBall.QueueFree();
        }
        currentBall = pinballScene.Instantiate<Pinball>();
        currentBall.Position = new Vector2(7600, 6500); // Set initial position
        AddChild(currentBall);
        // Subscribe to the instance BallOut event on the new ball. We unsubscribe from the old ball earlier.
        currentBall.BallOut -= OnBallOut; // safe no-op if not subscribed
        currentBall.BallOut += OnBallOut;
        GD.Print("Main: New ball spawned");
        // Ensure a RoundManager exists in the scene tree; if not, try to find one.
        if (RoundManager.Instance == null)
        {
            var rm = GetNodeOrNull<RoundManager>("/root/Root") ?? GetNodeOrNull<RoundManager>("RoundManager");
            if (rm != null)
                GD.Print("Main: Found RoundManager node; RoundManager will set Instance in its _Ready.");
            else
                GD.Print("Main: No RoundManager found in the scene tree. Add one to root or a known path to enable round events.");
        }
    }

private async void OnBallOut()
{
    if (isBallOut) return;
    isBallOut = true;
    GD.Print("Main: BallOut signal received");
    // Check if target was met
    if (score < targetScore)
    {
        roundFailed = true;
        GD.Print($"Main: Target not met. Score: {score}, Target: {targetScore}");
    }
    else
    {
        roundFailed = false;
        GD.Print($"Main: Target met. Score: {score}, Target: {targetScore}");
    }
    UpdateFailLabel();

    // Show fail label for 1.5 seconds if failed
    if (roundFailed)
    {
        await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
    }

    ResetScore();
    if (currentBall != null && IsInstanceValid(currentBall))
    {
        GD.Print("Main: Removing old ball");
        // Disconnect BallOut signal and clear reference
        try
        {
            currentBall.BallOut -= OnBallOut;
        }
        catch {}
        currentBall.QueueFree();
        currentBall = null;
    }
    // Notify managers via RoundManager instance if available
    if (RoundManager.Instance != null)
        RoundManager.Instance.NotifyBallOut();
    else
        GD.Print("[WARN] Main: RoundManager.Instance not found; unable to notify managers of BallOut.");
    // Also call BurnManager.StopBurn directly to preserve previous behavior and ensure burns stop.
    BurnManager.StopBurn();
    // Reset other resource managers on ball out for consistency
    ChargeManager.ResetCharge();
    GoldManager.ResetGold();
    // Instead of spawning a new ball immediately, show the UI
    GD.Print("Main: Showing PostRoundUI");
    if (!postRoundUI.Visible)
    {
        postRoundUI.ShowUI();
        placementGrid.Show(); // Show the placement grid
        placementGrid.AcceptClicks = false; // start disabled until a bumper is selected
    }
    // Do NOT increment round here
}

private void OnBumperSelected(PostRoundUI.BumperType bumperType, PackedScene bumperScene, Vector2I gridPosition)
{
        // If the cell is already occupied, re-open the UI and let the player pick another spot.
        var gridMgr = GridManager.Instance;
        if (gridMgr == null)
        {
            GD.PrintErr("Main: GridManager.Instance is null in OnBumperSelected.");
            return;
        }
        if (gridMgr.BumperGrid[gridPosition.X, gridPosition.Y] != null)
        {
            // Cell is occupied — ignore the click so the player can choose a different cell.
            return;
        }

        // Try to instantiate and place the bumper. Guard against invalid scene types.
        PopBumper newBumper = null;
        try
        {
            newBumper = bumperScene.Instantiate<PopBumper>();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Main: Failed to instantiate bumper scene: {ex.Message}");
            // Re-show UI so player can try a different bumper
            if (!postRoundUI.Visible)
                postRoundUI.ShowUI();
            placementGrid.Show();
            placementGrid.AcceptClicks = false;
            return;
        }

    gridMgr.BumperGrid[gridPosition.X, gridPosition.Y] = newBumper;
        newBumper.GridPosition = gridPosition;

    Vector2 cellSize = new Vector2(placementGrid.Size.X / gridMgr.Columns, placementGrid.Size.Y / gridMgr.Rows);
        Vector2 cellCenterLocal = new Vector2(
            gridPosition.X * cellSize.X + cellSize.X / 2,
            gridPosition.Y * cellSize.Y + cellSize.Y / 2
        );
        newBumper.GlobalPosition = placementGrid.GlobalPosition + cellCenterLocal;

        AddChild(newBumper);
        GD.Print($"Placed {bumperType} at grid {gridPosition}");

        // Successful placement: hide grid, advance round and spawn a new ball
        placementGrid.Hide();
        currentRound++;
        SpawnBall();
}

    private void OnPassiveSelected(IPassive passive)
    {
        GD.Print($"Main: Passive '{passive.Name}' selected");
        
        // Acquire the passive through PassiveManager
        if (PassiveManager.Instance != null)
        {
            PassiveManager.Instance.AcquirePassive(passive);
        }
        else
        {
            GD.PrintErr("Main: PassiveManager.Instance is null in OnPassiveSelected.");
        }
        
        // Start next round immediately without placement
        currentRound++;
        SpawnBall();
    }

    private void OnBurnScore(int amount)
    {
        // burnAmount is now managed by BurnManager
        UpdateBurnAmountLabel(); // Update the burn amount label based on BurnManager.BurnAmount
        AddScore(amount, false); // Burn should NOT be multiplied
        GD.Print($"Main: Burned score by {amount}. Total score: {Mathf.RoundToInt(score)}");
    }
}
