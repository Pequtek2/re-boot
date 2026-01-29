using Godot;
using System;

public partial class Machine : Area2D
{
	[ExportGroup("Settings")]
	[Export] public string MachineID = "machine_2"; // To ID musi się zgadzać z ID w NPC Inżyniera
	[Export] public string MachineName = "Taśmociąg";
	[Export] public string MinigameScenePath = "res://_Scenes/Minigames/Game2.tscn";
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
	
	// Zmienne stanu
	private bool _isLocked = true; 
	private bool _isFixed = false;

	public override void _Ready()
	{
		_iconE = GetNodeOrNull<Sprite2D>("IconE");
		_machineSprite = GetNodeOrNull<Sprite2D>("Sprite2D");

		GetNode<Area2D>("InteractionZone").BodyEntered += (body) => { if(body.Name == "Player") { _isPlayerNearby = true; UpdateVisuals(); } };
		GetNode<Area2D>("InteractionZone").BodyExited += (body) => { if(body.Name == "Player") { _isPlayerNearby = false; ClosePanel(); UpdateVisuals(); } };

		if (YesButton != null) YesButton.Pressed += OnYesPressed;
		if (NoButton != null) NoButton.Pressed += OnNoPressed;

		ClosePanel();
	}

	public override void _Process(double delta)
	{
		CheckLockState();
	}

	private void CheckLockState()
	{
		if (MainGameManager.Instance == null) return;

		// 1. Czy JA jestem naprawiona? (Nie obchodzi nas Maszyna 1!)
		_isFixed = MainGameManager.Instance.IsMachineFixed(MachineID);
		
		// 2. Czy Inżynier mnie odblokował?
		// Ta flaga (IsMachineUnlocked) zmienia się na TRUE tylko w momencie rozmowy z Inżynierem.
		bool unlockedByNPC = MainGameManager.Instance.IsMachineUnlocked(MachineID);
		
		if (_isFixed) 
		{
			_isLocked = false; // Naprawiona = Zawsze otwarta (zielona)
		}
		else 
		{
			// Jeśli nie jest naprawiona, to jedyny sposób na odblokowanie to zgoda NPC.
			// Stan naprawy Maszyny 1 NIE MA tu żadnego znaczenia.
			_isLocked = !unlockedByNPC; 
		}

		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		if (_machineSprite == null) return;

		if (_isFixed) _machineSprite.Modulate = Colors.Green;       
		else if (_isLocked) _machineSprite.Modulate = Colors.Gray;  
		else _machineSprite.Modulate = Colors.White;                

		if (_iconE != null)
		{
			_iconE.Visible = _isPlayerNearby && !_isFixed && !_isUIOpen;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_isPlayerNearby && !_isFixed && Input.IsActionJustPressed("interact"))
		{
			if (_isLocked)
			{
				// Jeśli Inżynier jeszcze nie odblokował -> Wyświetl komunikat o błędzie
				if (DialogueManager.Instance != null) 
					DialogueManager.Instance.StartDialogue(LockedDialogueID);
			}
			else
			{
				if (!_isUIOpen) OpenPanel();
				else ClosePanel();
			}
		}
	}

	private void OpenPanel() { _isUIOpen = true; if(ConfirmPanel != null) ConfirmPanel.Visible = true; if(MessageLabel != null) MessageLabel.Text = $"Naprawić: {MachineName}?"; }
	private void ClosePanel() { _isUIOpen = false; if(ConfirmPanel != null) ConfirmPanel.Visible = false; }
// W pliku Machine.cs

private void OnYesPressed() 
{ 
	// Sprawdź czy drzewo w ogóle istnieje
	var tree = GetTree();
	if (tree == null)
	{
		GD.PrintErr("BŁĄD: Maszyna utraciła połączenie z drzewem sceny!");
		return;
	}

	// Użyj CallDeferred dla bezpieczeństwa
	// Zamiast: tree.ChangeSceneToFile(MinigameScenePath);
	tree.CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MinigameScenePath);
}
	private void OnNoPressed() { ClosePanel(); }
}
