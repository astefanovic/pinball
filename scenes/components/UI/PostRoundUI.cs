using Godot;
using System;
using Godot.Collections;

public partial class PostRoundUI : Control
{
    public enum BumperType
    {
        PopBumper,
        BurnPopBumper,
        GoldPopBumper,
        ChargePopBumper,
        MultBumper,
        None
    }

    [Signal]
    public delegate void BumperSelectedEventHandler(BumperType bumperType, PackedScene bumperScene, Vector2I gridPosition);

    [Signal]
    public delegate void PassiveSelectedEventHandler(string passiveName);

    // C# event counterpart for strongly-typed subscription from Main.cs
    public event Action<BumperType, PackedScene, Vector2I> OnBumperSelectedEvent;
    public event Action<IPassive> OnPassiveSelectedEvent;

    private PackedScene[] _placeableBumperScenes;
    private PostRoundUI.BumperType[] _placeableBumperTypes;
    private PackedScene[] _randomBumperScenes = new PackedScene[3];
    private PostRoundUI.BumperType[] _randomBumperTypes = new PostRoundUI.BumperType[3];
    private Godot.Control.GuiInputEventHandler[] _panelHandlers = new Godot.Control.GuiInputEventHandler[3];
    private Panel[] _panelNodes;
    
    // Passive-related fields
    private IPassive[] _availablePassives;
    private IPassive[] _randomPassives = new IPassive[3];
    private Godot.Control.GuiInputEventHandler[] _passivePanelHandlers = new Godot.Control.GuiInputEventHandler[3];
    private Panel[] _passivePanelNodes;
    
    private BumperType _selectedBumperType;
    private PackedScene _selectedBumperScene;
    private IPassive _selectedPassive;
    private PlacementGrid _placementGridRef;

    public override void _EnterTree()
    {
    }

    public override void _ExitTree()
    {
    }

    public override void _Ready()
    {
        try
        {
            // Ready: basic initialization, logs removed for cleanliness.
            // PlacementGrid will be set externally by Main.cs
            
            // Only connect the signal once
            VisibilityChanged += _OnVisibilityChanged;

            _placeableBumperScenes = new PackedScene[] {
                GD.Load<PackedScene>("res://scenes/components/Placeable/Bumpers/PopBumper.tscn"),
                GD.Load<PackedScene>("res://scenes/components/Placeable/Bumpers/BurnPopBumper.tscn"),
                GD.Load<PackedScene>("res://scenes/components/Placeable/Bumpers/GoldPopBumper.tscn"),
                GD.Load<PackedScene>("res://scenes/components/Placeable/Bumpers/ChargePopBumper.tscn"),
                GD.Load<PackedScene>("res://scenes/components/Placeable/Bumpers/MultBumper.tscn")
            };
            _placeableBumperTypes = new PostRoundUI.BumperType[] {
                BumperType.PopBumper,
                BumperType.BurnPopBumper,
                BumperType.GoldPopBumper,
                BumperType.ChargePopBumper,
                BumperType.MultBumper
            };

            Node vbox = GetNodeOrNull("VBoxContainer");
            if (vbox is VBoxContainer vBoxContainer) // Cast to VBoxContainer
            {
                vBoxContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                vBoxContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            }
            else
            {
                GD.PrintErr("PostRoundUI: VBoxContainer not found or is not a VBoxContainer!");
            }

            // Set PostRoundUI to fill its parent
            SetAnchorsPreset(Control.LayoutPreset.FullRect);
            // Anchors set to fill parent.

            // Explicitly set the size of PostRoundUI itself (if it's not managed by its parent)
            // Note: Setting Size directly might be overridden by parent containers or layout.
            // If this node is a top-level UI element, setting its size might be appropriate.
            // Size = new Vector2(800, 400); // This might be redundant with FullRect preset
            // GD.Print($"PostRoundUI: Size set to {Size}");

            _panelNodes = new Panel[]
            {
                GetNodeOrNull<Panel>("VBoxContainer/BumperHBoxContainer/Panel"),
                GetNodeOrNull<Panel>("VBoxContainer/BumperHBoxContainer/Panel2"),
                GetNodeOrNull<Panel>("VBoxContainer/BumperHBoxContainer/Panel3")
            };
            
            _passivePanelNodes = new Panel[]
            {
                GetNodeOrNull<Panel>("VBoxContainer/PassiveHBoxContainer/PassivePanel"),
                GetNodeOrNull<Panel>("VBoxContainer/PassiveHBoxContainer/PassivePanel2"),
                GetNodeOrNull<Panel>("VBoxContainer/PassiveHBoxContainer/PassivePanel3")
            };
            
            // Set size flags and minimum size for each bumper panel
            foreach (var panel in _panelNodes)
            {
                if (panel != null)
                {
                    panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                    panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                    panel.CustomMinimumSize = new Vector2(200, 150); // Reduced height
                    panel.MouseFilter = Control.MouseFilterEnum.Stop;
                }
            }
            
            // Set size flags and minimum size for each passive panel
            foreach (var panel in _passivePanelNodes)
            {
                if (panel != null)
                {
                    panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                    panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                    panel.CustomMinimumSize = new Vector2(200, 150);
                    panel.MouseFilter = Control.MouseFilterEnum.Stop;
                }
            }
            // The redundant loop for panel node found/null is removed here.

            ZIndex = 10;
            SetupUI();
            // Hide();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"PostRoundUI: Error in _Ready(): {ex.Message}");
            GD.PrintErr($"PostRoundUI: Stack Trace: {ex.StackTrace}");
        }
    }

    public override void _Process(double delta)
    {
        if (Visible)
        {
            if (_panelNodes == null) return;
            // Continuously center the bumpers in case of layout changes
            foreach (var panel in _panelNodes)
            {
                if (panel == null) continue;
                foreach (Node child in panel.GetChildren())
                {
                    if (child is Node2D bumper)
                    {
                        bumper.Position = panel.Size / 2;
                    }
                }
            }
        }
    }

    public void ShowUI()
    {
        // Reset any previous selection and show UI
        _selectedBumperScene = null;
        _selectedBumperType = BumperType.None;
        _selectedPassive = null;
        Show();
        // Defer SetupUI so panels are repopulated after layout is ready
        CallDeferred(nameof(SetupUI));
    }

    // Debug helpers removed for cleanliness now that UI behavior is stable.

    public void SetupUI()
    {
        if (_panelNodes == null || _panelNodes.Length < 3 || _placeableBumperScenes == null || _placeableBumperScenes.Length < 5)
        {
            GD.PrintErr("PostRoundUI: SetupUI called before _Ready() or with missing panel/scene data.");
            return;
        }

        // Setup bumper selection
        SetupBumperSelection();
        
        // Setup passive selection
        SetupPassiveSelection();
    }
    
    private void SetupBumperSelection()
    {
        var indices = new System.Collections.Generic.List<int>();
        for (int i = 0; i < _placeableBumperScenes.Length; i++)
        {
            indices.Add(i);
        }
        var rng = new Random();
        for (int i = 0; i < 3; i++)
        {
            int idx = rng.Next(indices.Count);
            int bumperIdx = indices[idx];
            _randomBumperScenes[i] = _placeableBumperScenes[bumperIdx];
            _randomBumperTypes[i] = _placeableBumperTypes[bumperIdx];
            indices.RemoveAt(idx);
        }

        for (int i = 0; i < 3; i++)
        {
            if (_panelNodes[i] != null)
            {
                _SetupPanel(_panelNodes[i], _randomBumperScenes[i]);
                if (_panelHandlers[i] != null)
                {
                    _panelNodes[i].GuiInput -= _panelHandlers[i];
                }
                int panelIndex = i;
                _panelHandlers[i] = (e) => OnPanelClicked(e, _randomBumperTypes[panelIndex], _randomBumperScenes[panelIndex]);
                _panelNodes[i].GuiInput += _panelHandlers[i];
            }
        }
    }
    
    private void SetupPassiveSelection()
    {
        if (PassiveManager.Instance == null)
        {
            GD.PrintErr("PostRoundUI: PassiveManager not available for passive selection");
            return;
        }
        
        _availablePassives = PassiveManager.Instance.GetRandomPassivesForSelection();
        
        // Fill randomPassives array, padding with nulls if needed
        for (int i = 0; i < 3; i++)
        {
            if (i < _availablePassives.Length)
                _randomPassives[i] = _availablePassives[i];
            else
                _randomPassives[i] = null;
        }
        
        for (int i = 0; i < 3; i++)
        {
            if (_passivePanelNodes[i] != null)
            {
                _SetupPassivePanel(_passivePanelNodes[i], _randomPassives[i]);
                if (_passivePanelHandlers[i] != null)
                {
                    _passivePanelNodes[i].GuiInput -= _passivePanelHandlers[i];
                }
                int panelIndex = i;
                _passivePanelHandlers[i] = (e) => OnPassivePanelClicked(e, _randomPassives[panelIndex]);
                _passivePanelNodes[i].GuiInput += _passivePanelHandlers[i];
            }
        }
    }

    private void _OnVisibilityChanged()
    {
        // Only needed for hiding/cleanup now
        if (!Visible)
        {
            if (_panelNodes != null)
            {
                foreach (var panel in _panelNodes)
                {
                    _ClearPanel(panel);
                }
            }
            
            if (_passivePanelNodes != null)
            {
                foreach (var panel in _passivePanelNodes)
                {
                    _ClearPassivePanel(panel);
                }
            }
        }
    }
    // Removed OnVisibilityChanged override, not supported by Control

    private async void OnPanelClicked(InputEvent @event, BumperType selectedBumperType, PackedScene selectedBumperScene)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.Pressed)
        {
            _selectedBumperType = selectedBumperType;
            _selectedBumperScene = selectedBumperScene;
            // Now, hide the UI immediately after selection but defer enabling grid clicks
            Hide();
            if (_placementGridRef != null)
            {
                // Small delay so the current mouse event doesn't pass through to the grid
                await ToSignal(GetTree().CreateTimer(0.05f), "timeout");
                _placementGridRef.AcceptClicks = true;
            }
            // EmitSignal(SignalName.BumperSelected, (int)selectedBumperType, selectedBumperScene); // This signal is emitted OnGridCellClicked
        }
    }
    
    private void OnPassivePanelClicked(InputEvent @event, IPassive selectedPassive)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.Pressed)
        {
            if (selectedPassive != null)
            {
                _selectedPassive = selectedPassive;
                
                // Emit passive selected signal
                OnPassiveSelectedEvent?.Invoke(selectedPassive);
                EmitSignal(SignalName.PassiveSelected, selectedPassive.Name);
                
                // Hide the UI and start next round without placement
                Hide();
            }
        }
    }

    public void SetPlacementGrid(PlacementGrid grid)
    {
        if (grid != null)
        {
            _placementGridRef = grid;
            grid.GridCellClicked += OnGridCellClicked;
            // Ensure grid doesn't accept clicks until a bumper is selected
            grid.AcceptClicks = false;
        }
        else
        {
            GD.PrintErr("PostRoundUI: Attempted to set null PlacementGrid.");
        }
    }

    private void OnGridCellClicked(Vector2I cellCoords)
    {
        if (_selectedBumperScene != null)
        {
            // Notify listeners that the player attempted a placement at cellCoords. Do NOT clear selection here;
            // Main will respond by placing the bumper or by re-showing the UI when placement fails.
            OnBumperSelectedEvent?.Invoke(_selectedBumperType, _selectedBumperScene, cellCoords);
            EmitSignal(SignalName.BumperSelected, (int)_selectedBumperType, _selectedBumperScene, cellCoords);
        }
    }

    private void _SetupPanel(Panel panel, PackedScene bumperScene)
    {
        _ClearPanel(panel);

        if (bumperScene == null)
        {
            return;
        }

        Node2D bumperInstance = null;
        try
        {
            bumperInstance = (Node2D)bumperScene.Instantiate();
        }
        catch (Exception ex)
        {
            GD.PrintErr("PostRoundUI: Error instantiating bumperScene: " + ex.Message);
            return;
        }

        if (bumperInstance == null)
        {
            GD.PrintErr("PostRoundUI: Instantiated bumperInstance is null.");
            return;
        }

    // Adding bumper instance to panel

        // Prefer adding to the panel's CenterContainer if it exists (layout from tscn)
        Node center = panel.GetNodeOrNull("CenterContainer");
        if (center == null)
            center = panel.GetNodeOrNull("CenterContainer2");
        if (center == null)
            center = panel.GetNodeOrNull("CenterContainer3");

        if (center != null && center is Node nodeCenter)
        {
            nodeCenter.AddChild(bumperInstance);
        }
        else
        {
            panel.AddChild(bumperInstance);
        }

        // Ensure all visual CanvasItem descendants of the bumper are visible and drawn on top
        EnsureCanvasItemVisibilityRecursive(bumperInstance, true);

        // Center the visual children of the bumper inside the panel or center container
        CenterBumperVisualsInPanel(bumperInstance, panel);

    // After adding, position is computed in CenterBumperVisualsInPanel
    }
    
    private void _SetupPassivePanel(Panel panel, IPassive passive)
    {
        _ClearPassivePanel(panel);

        if (passive == null)
        {
            // Show empty panel with "Skip" option
            var skipLabel = new Label();
            skipLabel.Text = "Skip Passive";
            skipLabel.HorizontalAlignment = HorizontalAlignment.Center;
            skipLabel.VerticalAlignment = VerticalAlignment.Center;
            
            var centerContainer = panel.GetNodeOrNull("PassiveCenterContainer");
            if (centerContainer == null)
                centerContainer = panel.GetNodeOrNull("PassiveCenterContainer2");
            if (centerContainer == null)
                centerContainer = panel.GetNodeOrNull("PassiveCenterContainer3");
                
            if (centerContainer != null)
            {
                centerContainer.AddChild(skipLabel);
            }
            else
            {
                panel.AddChild(skipLabel);
            }
            return;
        }

        // Create visual representation for the passive
        var vbox = new VBoxContainer();
        
    var nameLabel = new Label();
    nameLabel.Text = passive.Name;
    nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
    // Make label large similar to FPSCounter font sizing
    nameLabel.AddThemeConstantOverride("font_size", 36);
    nameLabel.CustomMinimumSize = new Vector2(200, 48);
    vbox.AddChild(nameLabel);
        
    var descLabel = new Label();
    descLabel.Text = passive.Description;
    descLabel.HorizontalAlignment = HorizontalAlignment.Center;
    descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
    descLabel.AddThemeConstantOverride("font_size", 18);
    descLabel.CustomMinimumSize = new Vector2(200, 36);
    vbox.AddChild(descLabel);

        // Add to the appropriate center container
        var centerContainer2 = panel.GetNodeOrNull("PassiveCenterContainer");
        if (centerContainer2 == null)
            centerContainer2 = panel.GetNodeOrNull("PassiveCenterContainer2");
        if (centerContainer2 == null)
            centerContainer2 = panel.GetNodeOrNull("PassiveCenterContainer3");
            
        if (centerContainer2 != null)
        {
            centerContainer2.AddChild(vbox);
        }
        else
        {
            panel.AddChild(vbox);
        }

    // After adding, position is computed in CenterBumperVisualsInPanel
    }

    private void CenterBumperVisualsInPanel(Node2D bumperInstance, Panel panel)
    {
        if (bumperInstance == null || panel == null) return;

        var visuals = new System.Collections.Generic.List<Node>();
        CollectCanvasItemVisuals(bumperInstance, visuals);

        if (visuals.Count == 0)
        {
            // fallback: center the bumper root
            // If the bumper was added to a CenterContainer (Control), center relative to that instead
            if (bumperInstance.GetParent() is Control parentControl)
                bumperInstance.Position = parentControl.Size / 2;
            else
                bumperInstance.Position = panel.Size / 2;
            return;
        }

        // Compute bounding box (min/max) of visuals relative to bumperInstance
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (var v in visuals)
        {
            var localPos = GetLocalPositionRelativeTo(v, bumperInstance);
            if (v is Control ctrl)
            {
                // Consider control rect size
                Vector2 size = ctrl.Size; // Control.Size is available
                minX = Math.Min(minX, localPos.X);
                minY = Math.Min(minY, localPos.Y);
                maxX = Math.Max(maxX, localPos.X + size.X);
                maxY = Math.Max(maxY, localPos.Y + size.Y);
            }
            else if (v is Node2D)
            {
                // Use the node's position as a point (best-effort)
                minX = Math.Min(minX, localPos.X);
                minY = Math.Min(minY, localPos.Y);
                maxX = Math.Max(maxX, localPos.X);
                maxY = Math.Max(maxY, localPos.Y);
            }
        }

        if (minX == float.MaxValue || minY == float.MaxValue)
        {
            // Fallback: center the root
            if (bumperInstance.GetParent() is Control parentCtrl)
                bumperInstance.Position = parentCtrl.Size / 2;
            else
                bumperInstance.Position = panel.Size / 2;
            return;
        }

        Vector2 bboxCenter = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);

        // Position bumper so that bounding-box center aligns with the parent container center
        Vector2 targetCenter;
        if (bumperInstance.GetParent() is Control parentControl2)
            targetCenter = parentControl2.Size / 2f;
        else
            targetCenter = panel.Size / 2f;

        bumperInstance.Position = targetCenter - bboxCenter;
    }

    private void CollectCanvasItemVisuals(Node node, System.Collections.Generic.List<Node> outList)
    {
        if (node == null) return;
        if (node is CanvasItem)
        {
            outList.Add(node);
        }

        foreach (Node child in node.GetChildren())
        {
            CollectCanvasItemVisuals(child, outList);
        }
    }

    private Vector2 GetLocalPositionRelativeTo(Node node, Node ancestor)
    {
        // Walk up the parent chain accumulating positions until we reach ancestor
        Vector2 pos = Vector2.Zero;
        Node current = node;
        while (current != null && current != ancestor)
        {
            if (current is Node2D n2d)
            {
                pos += n2d.Position;
            }
            else if (current is Control ctrl)
            {
                pos += ctrl.Position;
            }
            current = current.GetParent();
        }

        // If we reached ancestor, pos represents position relative to ancestor
        // If not, return Vector2.Zero as fallback
        if (current == ancestor)
            return pos;
        return Vector2.Zero;
    }

    private void _ClearPanel(Panel panel)
    {
        if (panel == null) return;
        // First, clear any bumper instances inside known CenterContainer children
        string[] centerNames = new string[] { "CenterContainer", "CenterContainer2", "CenterContainer3" };
        foreach (var name in centerNames)
        {
            var center = panel.GetNodeOrNull(name);
            if (center != null)
            {
                foreach (Node child in center.GetChildren().Duplicate())
                {
                    if (child is Node2D)
                    {
                        center.RemoveChild(child);
                        child.Free();
                    }
                }
            }
        }

        // Also clear any direct Node2D children of the panel itself (fallback)
        foreach (Node child in panel.GetChildren().Duplicate())
        {
            if (child is Node2D)
            {
                panel.RemoveChild(child);
                child.Free();
            }
        }
    }

    private void EnsureCanvasItemVisibilityRecursive(Node node, bool visible)
    {
        if (node is CanvasItem ci)
        {
            ci.Visible = visible;
            try
            {
                // Node2D/CanvasItem expose ZIndex; set it high so preview renders above panel background
                ci.ZIndex = 1000;
            }
            catch { }
        }

        foreach (Node child in node.GetChildren())
        {
            EnsureCanvasItemVisibilityRecursive(child, visible);
        }
    }
    
    private void _ClearPassivePanel(Panel panel)
    {
        if (panel == null) return;
        
        // Clear any passive instances inside known CenterContainer children
        string[] centerNames = new string[] { "PassiveCenterContainer", "PassiveCenterContainer2", "PassiveCenterContainer3" };
        foreach (var name in centerNames)
        {
            var center = panel.GetNodeOrNull(name);
            if (center != null)
            {
                foreach (Node child in center.GetChildren().Duplicate())
                {
                    center.RemoveChild(child);
                    child.Free();
                }
            }
        }

        // Also clear any direct children of the panel itself (fallback)
        foreach (Node child in panel.GetChildren().Duplicate())
        {
            if (child is Control)
            {
                panel.RemoveChild(child);
                child.Free();
            }
        }
    }
}