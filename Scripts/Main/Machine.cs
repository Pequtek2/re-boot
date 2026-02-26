using Godot;
using System;

public partial class Machine : Area2D
{
	[ExportGroup("Settings")]
	[Export] public string MachineID = "machine_2"; 
	[Export] public string MachineName = "Taśmociąg";
	[Export] public string MinigameScenePath = "res://Scenes/Minigames/InstantFix.tscn"; 
	[Export] public string LockedDialogueID = "system_locked_2"; 

	[ExportGroup("UI References")]
	[Export] public Control ConfirmPanel; 
	[Export] public Label MessageLabel; 
	[Export] public Button YesButton;
	[Export] public Button NoButton;

	private Sprite2D _iconE;
	private Sprite2D _machineSprite;
	private bool _isPlayerNearby = false;
	private bool _isUIOpen = false;
	
	private bool _isLocked = true; 
	private bool _isFixed = false;

	public override void _Ready()
	{
		_iconE = GetNodeOrNull<Sprite2D>("IconE");
		_machineSprite = GetNodeOrNull<Sprite2D>("Sprite2D");

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
		if (_machineSprite != null)
		{
			if (_isFixed) _machineSprite.Modulate = Colors.Green;       
			else if (_isLocked) _machineSprite.Modulate = Colors.Gray;  
			else _machineSprite.Modulate = Colors.White;                
		}

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
		// --- POPRAWKA KRYTYCZNA ---
		// Jeśli panel nie został otwarty przez TĘ KONKRETNĄ maszynę, ignoruj kliknięcie.
		// Dzięki temu, mimo że 4 maszyny słuchają przycisku, zareaguje tylko ta jedna aktywna.
		if (!_isUIOpen) return;
		
		if (MainGameManager.Instance == null) return;
		
		// Zabezpieczenie przed błędem "tree is null"
		if (GetTree() == null) return;

		ClosePanel();

		// --- DIAGNOSTYKA ---
		GD.Print($"[MACHINE] Kliknięto TAK dla maszyny: {MachineID}");

		// --- SZUKANIE GRACZA ---
		Node2D player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
		
		// Fallback (szukanie po nazwie)
		if (player == null && GetTree().CurrentScene != null)
		{
			player = GetTree().CurrentScene.FindChild("Player", true, false) as Node2D;
		}

		Vector2 playerPos = Vector2.Zero;
		if (player != null) playerPos = player.GlobalPosition;

		string currentScene = GetTree().CurrentScene.SceneFilePath;

		// Zmiana sceny
		MainGameManager.Instance.SwitchToMinigame(MachineID, MinigameScenePath, playerPos, currentScene);
	}

	private void OnNoPressed() 
	{ 
		// Tutaj też to dodajemy, żeby inne maszyny nie zamykały panelu "wirtualnie"
		if (!_isUIOpen) return;
		
		ClosePanel(); 
	}
}
