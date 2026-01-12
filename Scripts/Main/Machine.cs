using Godot;
using System;

public partial class Machine : Area2D
{
	[ExportGroup("Settings")]
	[Export] public string MachineID = "machine_2"; // Upewnij się, że to ID pasuje do tego w NPC!
	[Export] public string MachineName = "Taśmociąg";
	[Export] public string MinigameScenePath = "res://_Scenes/Minigames/Game2.tscn";
	
	// Dialog wyświetlany, gdy klikniesz ZANIM Inżynier odblokuje maszynę
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
	
	// Stany
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
		// Sprawdzamy stan co klatkę, żeby reagować natychmiast na rozmowę z NPC
		CheckLockState();
	}

	private void CheckLockState()
	{
		if (MainGameManager.Instance == null) return;

		// 1. Sprawdzamy czy JA jestem naprawiona
		_isFixed = MainGameManager.Instance.IsMachineFixed(MachineID);
		
		// 2. KLUCZOWA ZMIANA: Ignorujemy inne maszyny. 
		// Sprawdzamy TYLKO czy NPC nas odblokował w Managerze.
		bool unlockedByNPC = MainGameManager.Instance.IsMachineUnlocked(MachineID);
		
		if (_isFixed) 
		{
			_isLocked = false; // Jak naprawiona, to odblokowana na stałe (zielona)
		}
		else 
		{
			// Jeśli nie jest naprawiona, to jej stan zależy WYŁĄCZNIE od NPC.
			// Jeśli NPC nie zagadał (unlockedByNPC == false), to maszyna jest ZABLOKOWANA.
			_isLocked = !unlockedByNPC; 
		}

		UpdateVisuals();
	}

	private void UpdateVisuals()
	{
		if (_machineSprite == null) return;

		if (_isFixed) _machineSprite.Modulate = Colors.Green;       
		else if (_isLocked) _machineSprite.Modulate = Colors.Gray;  // Szara = zablokowana
		else _machineSprite.Modulate = Colors.White;                // Biała = gotowa do gry

		if (_iconE != null)
		{
			// Ikonka widoczna, jeśli podejdziemy, nawet jak zablokowana (żeby kliknąć i dostać info)
			_iconE.Visible = _isPlayerNearby && !_isFixed && !_isUIOpen;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_isPlayerNearby && !_isFixed && Input.IsActionJustPressed("interact"))
		{
			if (_isLocked)
			{
				// Maszyna zablokowana -> Wyświetl komunikat "Błąd systemu / Idź do Inżyniera"
				if (DialogueManager.Instance != null) 
					DialogueManager.Instance.StartDialogue(LockedDialogueID);
			}
			else
			{
				// Maszyna odblokowana -> Otwórz panel naprawy
				if (!_isUIOpen) OpenPanel();
				else ClosePanel();
			}
		}
	}

	private void OpenPanel() { _isUIOpen = true; if(ConfirmPanel != null) ConfirmPanel.Visible = true; if(MessageLabel != null) MessageLabel.Text = $"Naprawić: {MachineName}?"; }
	private void ClosePanel() { _isUIOpen = false; if(ConfirmPanel != null) ConfirmPanel.Visible = false; }
	private void OnYesPressed() { GetTree().ChangeSceneToFile(MinigameScenePath); }
	private void OnNoPressed() { ClosePanel(); }
}
