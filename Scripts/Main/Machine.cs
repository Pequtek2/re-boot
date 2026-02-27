using Godot;
using System;

public partial class Machine : Area2D
{
	[ExportGroup("Settings")]
	[Export] public string MachineID = "machine_2"; 
	[Export] public string MachineName = "Taśmociąg";
	[Export] public string MinigameScenePath = "res://Scenes/Minigames/InstantFix.tscn"; 
	[Export] public string LockedDialogueID = "system_locked_2"; 

	[ExportGroup("Visuals & Textures")]
	[Export] public Texture2D BrokenTexture; 
	[Export] public Texture2D FixedTexture;  
	[Export] public string FloatingDescription = "Krótki opis maszyny"; 
	
	[ExportGroup("Floating Text Animation")]
	[Export] public float FloatAmplitude = 5.0f; 
	[Export] public float FloatSpeed = 3.0f;     

	[ExportGroup("UI References")]
	[Export] public Control ConfirmPanel; 
	[Export] public Label MessageLabel; 
	[Export] public Button YesButton;
	[Export] public Button NoButton;

	private Sprite2D _iconE;
	private Sprite2D _machineSprite;
	private Label _floatingLabel; 
	
	private bool _isPlayerNearby = false;
	private bool _isUIOpen = false;
	
	private bool _isLocked = true; 
	private bool _isFixed = false;

	private float _originalLabelY;
	private double _timePassed = 0.0;

	public override void _Ready()
	{
		_iconE = GetNodeOrNull<Sprite2D>("IconE");
		_machineSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		_floatingLabel = GetNodeOrNull<Label>("FloatingLabel"); 

		if (_floatingLabel != null)
		{
			_floatingLabel.Text = FloatingDescription;
			_originalLabelY = _floatingLabel.Position.Y; 
		}

		var interactionZone = GetNodeOrNull<Area2D>("InteractionZone");
		if (interactionZone != null)
		{
			interactionZone.BodyEntered += (body) => { if(body.Name == "Player" || body.IsInGroup("Player")) { _isPlayerNearby = true; UpdateVisuals(); } };
			interactionZone.BodyExited += (body) => { if(body.Name == "Player" || body.IsInGroup("Player")) { _isPlayerNearby = false; ClosePanel(); UpdateVisuals(); } };
		}
		else
		{
			BodyEntered += (body) => { if(body.Name == "Player" || body.IsInGroup("Player")) { _isPlayerNearby = true; UpdateVisuals(); } };
			BodyExited += (body) => { if(body.Name == "Player" || body.IsInGroup("Player")) { _isPlayerNearby = false; ClosePanel(); UpdateVisuals(); } };
		}

		if (YesButton != null) YesButton.Pressed += OnYesPressed;
		if (NoButton != null) NoButton.Pressed += OnNoPressed;

		ClosePanel();
		UpdateVisuals();
	}

	public override void _Process(double delta)
	{
		CheckLockState();

		if (_floatingLabel != null && _floatingLabel.Visible)
		{
			_timePassed += delta;
			float newY = _originalLabelY + Mathf.Sin((float)_timePassed * FloatSpeed) * FloatAmplitude;
			_floatingLabel.Position = new Vector2(_floatingLabel.Position.X, newY);
		}
	}

	private void CheckLockState()
	{
		if (MainGameManager.Instance == null) return;
		_isFixed = MainGameManager.Instance.IsMachineFixed(MachineID);
		
		bool unlockedByNPC = MainGameManager.Instance.IsMachineUnlocked(MachineID);
		
		if (_isFixed) _isLocked = false; 
		else _isLocked = !unlockedByNPC; 

		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		// --- Aktualizacja Grafiki Maszyny ---
		if (_machineSprite != null)
		{
			if (_isFixed)
			{
				if (FixedTexture != null) _machineSprite.Texture = FixedTexture;
				_machineSprite.Modulate = Colors.White; 
			}
			else
			{
				if (BrokenTexture != null) _machineSprite.Texture = BrokenTexture;
				
				if (_isLocked) _machineSprite.Modulate = Colors.Gray;  
				else _machineSprite.Modulate = Colors.White;                
			}
		}

		// --- Aktualizacja Lewitującego Tekstu ---
		if (_floatingLabel != null)
		{
			if (_isFixed)
			{
				// Zmiana tekstu i koloru na zielony po naprawie
				_floatingLabel.Text = $"{FloatingDescription} ✓";
				_floatingLabel.Modulate = Colors.LimeGreen; // LimeGreen jest zazwyczaj jaśniejszy i czytelniejszy niż zwykły Green
			}
			else
			{
				// Powrót do standardowego wyglądu, jeśli maszyna jest zepsuta
				_floatingLabel.Text = FloatingDescription;
				_floatingLabel.Modulate = Colors.White; 
			}
		}

		// --- Aktualizacja Ikonki 'E' ---
		if (_iconE != null)
			_iconE.Visible = _isPlayerNearby && !_isFixed && !_isUIOpen;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_isPlayerNearby && !_isFixed && Input.IsActionJustPressed("interact"))
		{
			if (_isLocked)
			{
				if (DialogueManager.Instance != null) 
					DialogueManager.Instance.StartDialogue(LockedDialogueID);
				else
					GD.Print($"[Machine] Zablokowane. ID: {MachineID}");
			}
			else
			{
				if (!_isUIOpen) OpenPanel();
				else ClosePanel();
			}
		}
	}

	private void OpenPanel() 
	{ 
		_isUIOpen = true; 
		if(ConfirmPanel != null) ConfirmPanel.Visible = true; 
		if(MessageLabel != null) MessageLabel.Text = $"Naprawić: {MachineName}?"; 
	}

	private void ClosePanel() 
	{ 
		_isUIOpen = false; 
		if(ConfirmPanel != null) ConfirmPanel.Visible = false; 
	}

	private void OnYesPressed() 
	{ 
		if (!_isUIOpen) return;
		if (MainGameManager.Instance == null) return;
		if (GetTree() == null) return;

		ClosePanel();
		GD.Print($"[MACHINE] Kliknięto TAK dla maszyny: {MachineID}");

		Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		if (player == null && GetTree().CurrentScene != null)
		{
			player = GetTree().CurrentScene.FindChild("Player", true, false) as Node2D;
		}

		Vector2 playerPos = Vector2.Zero;
		if (player != null) playerPos = player.GlobalPosition;

		string currentScene = GetTree().CurrentScene.SceneFilePath;
		MainGameManager.Instance.SwitchToMinigame(MachineID, MinigameScenePath, playerPos, currentScene);
	}

	private void OnNoPressed() 
	{ 
		if (!_isUIOpen) return;
		ClosePanel(); 
	}
}
