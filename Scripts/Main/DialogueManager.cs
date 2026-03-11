using Godot;
using System;
using System.Collections.Generic;

public partial class DialogueManager : CanvasLayer
{
	public static DialogueManager Instance { get; private set; }

	[Export] public Label NameLabel;
	[Export] public RichTextLabel TextLabel;
	[Export] public TextureRect PortraitRect;
	[Export] public Control DialoguePanel;
	[Export] public string PortraitsPath = "res://Art/Portraits/";

	// Ustawienia animacji skakania
	[Export] public float BounceHeight = 5.0f; // Jak wysoko skacze (w pikselach)
	[Export] public float BounceSpeed = 20.0f; // Jak szybko skacze

	private Dictionary<string, Godot.Collections.Array> _dialogueDatabase;
	private List<Godot.Collections.Dictionary> _currentLines = new List<Godot.Collections.Dictionary>();
	
	private int _currentIndex = 0;
	private bool _isActive = false;
	private double _visibleRatio = 0.0;
	
	// Zmienne do obsługi portretu
	private Vector2 _portraitBasePos; // Zapamiętujemy, gdzie obrazek stoi normalnie
	private string _currentNpcId = ""; // Kto teraz gada?

	public override void _Ready()
	{
		Instance = this;
		LoadDatabase();
		
		if (DialoguePanel != null) DialoguePanel.Visible = false;
		
		// Zapamiętaj oryginalną pozycję obrazka (żeby wiedzieć, gdzie wracać po skoku)
		if (PortraitRect != null) 
		{
			_portraitBasePos = PortraitRect.Position;
			PortraitRect.Visible = false;
		}
	}

	private void LoadDatabase()
	{
		string path = "res://Dialogue/dialogues.json"; // Upewnij się co do ścieżki!
		
		if (!FileAccess.FileExists(path))
		{
			GD.PrintErr($"BŁĄD: Nie znaleziono pliku: {path}");
			return;
		}

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var json = new Json();
		var error = json.Parse(file.GetAsText());
		
		if (error == Error.Ok)
		{
			var rawData = (Godot.Collections.Dictionary)json.Data;
			_dialogueDatabase = new Dictionary<string, Godot.Collections.Array>();

			foreach (var keyVariant in rawData.Keys)
			{
				string key = (string)keyVariant;
				_dialogueDatabase.Add(key, (Godot.Collections.Array)rawData[key]);
			}
			GD.Print("Załadowano dialogi. Ilość wpisów: " + _dialogueDatabase.Count);
		}
		else
		{
			GD.PrintErr($"BŁĄD PARSOWANIA JSON: {json.GetErrorMessage()} w linii {json.GetErrorLine()}");
		}
	}

	public void StartDialogue(string npcId)
	{
		// Zabezpieczenie przed "spamowaniem" klawisza akcji podczas trwania dialogu
		if (_isActive) return;

		// Sprawdź, czy baza danych w ogóle się załadowała
		if (_dialogueDatabase == null)
		{
			GD.PrintErr("BŁĄD: Baza dialogów jest pusta! Sprawdź plik dialogues.json.");
			GetTree().Paused = false; 
			return;
		}

		// Sprawdź, czy mamy dialog dla tego NPC/ID
		if (!_dialogueDatabase.ContainsKey(npcId))
		{
			GD.PrintErr($"BŁĄD: Nie znaleziono dialogu o ID: '{npcId}'!");
			GD.Print("Dostępne ID: " + string.Join(", ", _dialogueDatabase.Keys));
			
			GetTree().Paused = false; 
			return;
		}

		// Zablokowanie poruszania się Gracza na czas rozmowy
		MainGameManager.Instance?.SetPlayerMovementBlocked(true);

		_currentNpcId = npcId; 

		// Automatyczne ładowanie portretu
		try { SetPortrait(npcId); } catch { /* ignoruj błąd braku obrazka */ }

		var variants = _dialogueDatabase[npcId];
		Godot.Collections.Dictionary bestMatch = null;

		// Szukamy pasującego wariantu dialogu
		foreach (var variantObj in variants)
		{
			var variant = (Godot.Collections.Dictionary)variantObj;
			if (CheckConditions(variant))
			{
				bestMatch = variant;
				break;
			}
		}

		// Jeśli znaleźliśmy pasujący - gramy go. Jeśli nie - default.
		if (bestMatch != null && bestMatch.ContainsKey("lines"))
		{
			PlayLines((Godot.Collections.Array)bestMatch["lines"]);
		}
		else
		{
			PlayDefaultMessage(npcId);
		}
	}

	private void SetPortrait(string imageName)
	{
		if (PortraitRect == null) return;

		string fullPath = PortraitsPath + imageName + ".png";
		if (ResourceLoader.Exists(fullPath))
		{
			PortraitRect.Texture = ResourceLoader.Load<Texture2D>(fullPath);
			PortraitRect.Visible = true;
			PortraitRect.Position = _portraitBasePos; 
		}
	}

	private bool CheckConditions(Godot.Collections.Dictionary variant)
	{
		if (variant.ContainsKey("requires"))
		{
			string reqs = (string)variant["requires"];
			foreach (var cond in reqs.Split(','))
				if (!CheckSingleCondition(cond.Trim(), true)) return false;
		}
		if (variant.ContainsKey("excludes"))
		{
			string excls = (string)variant["excludes"];
			foreach (var cond in excls.Split(','))
				if (CheckSingleCondition(cond.Trim(), false)) return false;
		}
		return true;
	}

	private bool CheckSingleCondition(string tag, bool expectedValue)
	{
		if (tag.StartsWith("quest_active:"))
		{
			var s = QuestManager.Instance?.GetQuestState(tag.Split(':')[1]);
			return (s != null && !s.IsCompleted);
		}
		else if (tag.StartsWith("quest_done:"))
		{
			var s = QuestManager.Instance?.GetQuestState(tag.Split(':')[1]);
			return (s != null && s.IsCompleted);
		}
		else if (tag.StartsWith("machine_fixed:"))
		{
			return MainGameManager.Instance != null && MainGameManager.Instance.IsMachineFixed(tag.Split(':')[1]);
		}
		return TagManager.Instance != null && TagManager.Instance.HasTag(tag);
	}

	private void PlayLines(Godot.Collections.Array linesRaw)
	{
		_currentLines.Clear();
		foreach (var l in linesRaw) _currentLines.Add((Godot.Collections.Dictionary)l);
		
		_currentIndex = 0;
		_isActive = true;
		if (DialoguePanel != null) DialoguePanel.Visible = true;
		ShowNextLine();
	}

	private void PlayDefaultMessage(string npcName)
	{
		var dict = new Godot.Collections.Dictionary();
		dict["name"] = npcName;
		dict["text"] = "...";
		_currentLines.Clear();
		_currentLines.Add(dict);
		
		_currentIndex = 0;
		_isActive = true;
		if (DialoguePanel != null) DialoguePanel.Visible = true;
		ShowNextLine();
	}

	private void ShowNextLine()
	{
		if (_currentIndex < _currentLines.Count)
		{
			var line = _currentLines[_currentIndex];
			
			if (NameLabel != null) NameLabel.Text = (string)line.GetValueOrDefault("name", "???");
			if (TextLabel != null)
			{
				TextLabel.Text = (string)line.GetValueOrDefault("text", "...");
				TextLabel.VisibleRatio = 0;
			}
			_visibleRatio = 0;

			// Obsługa portretu
			if (line.ContainsKey("portrait"))
			{
				SetPortrait((string)line["portrait"]);
			}

			if (line.ContainsKey("command")) ExecuteDialogueCommand((string)line["command"]);

			_currentIndex++;
		}
		else
		{
			EndDialogue();
		}
	}

	private void EndDialogue()
	{
		_isActive = false;
		if (DialoguePanel != null) DialoguePanel.Visible = false;

		// Odblokowanie Gracza po zakończeniu dialogu
		if (MainGameManager.Instance != null)
		{
			MainGameManager.Instance.SetPlayerMovementBlocked(false);
		}
	}

	private void ExecuteDialogueCommand(string commandsRaw)
	{
		var commands = commandsRaw.Split(',');

		foreach (var cmd in commands)
		{
			var parts = cmd.Trim().Split(':');
			string action = parts[0];

			switch (action)
			{
				case "start_quest": if (parts.Length >= 2) QuestManager.Instance?.StartQuest(parts[1]); break;
				case "progress_quest": 
					int amt = 1;
					if (parts.Length > 2) int.TryParse(parts[2], out amt);
					if (parts.Length >= 2) QuestManager.Instance?.ProgressQuest(parts[1], amt); 
					break;
				case "finish_quest": if (parts.Length >= 2) QuestManager.Instance?.ForceCompleteQuest(parts[1]); break;
				case "give_item": if (parts.Length >= 2) NotificationUI.Instance?.AddNotification("OTRZYMANO PRZEDMIOT", parts[1]); break;
				case "add_tag": if (parts.Length >= 2) TagManager.Instance?.AddTag(parts[1]); break;
				case "show_ending": 
					EndingScreen.Instance.StartEndingSequence(); 
					break;
				case "unlock_machine": if (parts.Length >= 2) MainGameManager.Instance?.UnlockMachine(parts[1]); break;
				case "show_controls":
					if (ControlsWindow.Instance != null)
					{
						ControlsWindow.Instance.ForceShow();
					}
					break;
			}
		}
	}

	// Obsługa sterowania - blokada ESC i przyspieszanie tekstu myszką/akcją
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_isActive) return;

		// Ignorujemy klawisz wyjścia (ESC), żeby nie dało się zamknąć dialogu!
		if (@event.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			return;
		}

		bool isAccept = @event.IsActionPressed("ui_accept") || @event.IsActionPressed("interact");
		bool isMouseClick = (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left);

		if (isAccept || isMouseClick)
		{
			GetViewport().SetInputAsHandled();

			// PRZYSPIESZENIE TEKSTU: jeśli wciąż się pisze, wymuś pojawienie się całości
			if (TextLabel != null && TextLabel.VisibleRatio < 1.0f) 
			{
				_visibleRatio = 1.0;
				TextLabel.VisibleRatio = 1.0f;
				return; 
			}

			// Jeżeli tekst się już napisał do końca, przejdź do kolejnej linijki
			ShowNextLine();
		}
	}

	public override void _Process(double delta)
	{
		if (!_isActive) return;

		// 1. Logika pisania tekstu
		bool isTyping = false;
		if (TextLabel != null && TextLabel.VisibleRatio < 1.0)
		{
			_visibleRatio += delta * 2.0; 
			TextLabel.VisibleRatio = (float)_visibleRatio;
			isTyping = true;
		}

		// 2. --- ANIMACJA SKAKANIA ---
		if (PortraitRect != null && PortraitRect.Visible)
		{
			if (isTyping)
			{
				float bounce = Mathf.Abs(Mathf.Sin(Time.GetTicksMsec() / 100.0f * (BounceSpeed / 10.0f))) * BounceHeight;
				PortraitRect.Position = new Vector2(_portraitBasePos.X, _portraitBasePos.Y - bounce);
			}
			else
			{
				PortraitRect.Position = _portraitBasePos; 
			}
		}
	}
}
