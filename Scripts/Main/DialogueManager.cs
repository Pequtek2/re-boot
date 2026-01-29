using Godot;
using System;
using System.Collections.Generic; // Używamy tego do List<> i Dictionary<,>
// USUNIĘTO: using Godot.Collections; aby uniknąć konfliktów

public partial class DialogueManager : CanvasLayer
{
	public static DialogueManager Instance { get; private set; }

	[Export] public Label NameLabel;
	[Export] public RichTextLabel TextLabel;
	[Export] public TextureRect PortraitRect;
	[Export] public Control DialoguePanel;
	[Export] public string PortraitsPath = "res://Art/Portraits/";

	// Baza dialogów: Klucz (ID NPC) -> Wartość (Godot Array wariantów dialogowych)
	// Używamy System.Collections.Generic.Dictionary dla głównej bazy
	private Dictionary<string, Godot.Collections.Array> _dialogueDatabase;
	
	// Aktualna sesja: Lista linii (jako Godot Dictionaries)
	private List<Godot.Collections.Dictionary> _currentLines = new List<Godot.Collections.Dictionary>();
	
	private int _currentIndex = 0;
	private bool _isActive = false;
	private double _visibleRatio = 0.0;

	public override void _Ready()
	{
		Instance = this;
		LoadDatabase();
		
		if (DialoguePanel != null) DialoguePanel.Visible = false;
		if (PortraitRect != null) PortraitRect.Visible = false;
	}

	private void LoadDatabase()
	{
		string path = "res://Dialogue/dialogues.json";
		if (!FileAccess.FileExists(path))
		{
			GD.PrintErr($"Brak pliku dialogów: {path}");
			return;
		}

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		string content = file.GetAsText();
		var json = new Json();
		
		if (json.Parse(content) == Error.Ok)
		{
			// Rzutujemy wynik na Godot.Collections.Dictionary (bo to zwraca JSON)
			var rawData = (Godot.Collections.Dictionary)json.Data;
			
			// Inicjalizujemy naszą bazę (C# Dictionary)
			_dialogueDatabase = new Dictionary<string, Godot.Collections.Array>();

			foreach (var keyVariant in rawData.Keys)
			{
				string key = (string)keyVariant; // ID NPC np. "kierownik"
				var variantList = (Godot.Collections.Array)rawData[key];
				
				_dialogueDatabase.Add(key, variantList);
			}
		}
		else
		{
			GD.PrintErr("Błąd w pliku JSON: " + json.GetErrorMessage());
		}
	}

	public void StartDialogue(string npcId)
	{
		if (_dialogueDatabase == null || !_dialogueDatabase.ContainsKey(npcId))
		{
			GD.PrintErr($"Nie znaleziono dialogów dla NPC: {npcId}");
			return;
		}

		// Pobieramy listę wariantów (Godot Array)
		var variants = _dialogueDatabase[npcId];
		Godot.Collections.Dictionary bestMatch = null;

		// Szukamy pasującego wariantu (od góry do dołu)
		foreach (var variantObj in variants)
		{
			var variant = (Godot.Collections.Dictionary)variantObj;
			if (CheckConditions(variant))
			{
				bestMatch = variant;
				break; // Znaleziono pierwszy pasujący!
			}
		}

		if (bestMatch != null)
		{
			// Pobieramy linie tekstu dla tego wariantu
			var lines = (Godot.Collections.Array)bestMatch["lines"];
			PlayLines(lines);
		}
		else
		{
			PlayDefaultMessage(npcId);
		}
	}

	private bool CheckConditions(Godot.Collections.Dictionary variant)
	{
		// 1. Sprawdź wymagania (requires)
		if (variant.ContainsKey("requires"))
		{
			string reqs = (string)variant["requires"];
			var list = reqs.Split(',');
			foreach (var cond in list)
			{
				if (!CheckSingleCondition(cond.Trim(), true)) return false;
			}
		}

		// 2. Sprawdź wykluczenia (excludes)
		if (variant.ContainsKey("excludes"))
		{
			string excls = (string)variant["excludes"];
			var list = excls.Split(',');
			foreach (var cond in list)
			{
				// Jeśli znajdziemy tag, który jest wykluczony -> odrzucamy dialog
				if (CheckSingleCondition(cond.Trim(), false)) return false; 
			}
		}

		return true;
	}

	private bool CheckSingleCondition(string tag, bool expectedValue)
	{
		bool hasTag = false;

		if (tag.StartsWith("quest_active:"))
		{
			string qId = tag.Split(':')[1];
			var state = QuestManager.Instance?.GetQuestState(qId);
			hasTag = (state != null && !state.IsCompleted);
		}
		else if (tag.StartsWith("quest_done:"))
		{
			string qId = tag.Split(':')[1];
			var state = QuestManager.Instance?.GetQuestState(qId);
			hasTag = (state != null && state.IsCompleted);
		}
		else if (tag.StartsWith("machine_fixed:"))
	{
		string mId = tag.Split(':')[1];
		// Sprawdzamy w MainGameManagerze czy maszyna jest naprawiona
		hasTag = MainGameManager.Instance != null && MainGameManager.Instance.IsMachineFixed(mId);
	}
		else
		{
			// Sprawdzamy zwykłe tagi w TagManagerze
			hasTag = TagManager.Instance != null && TagManager.Instance.HasTag(tag);
		}

		// Dla 'requires' (expected=true): musimy mieć tag (return hasTag).
		// Dla 'excludes' (expected=false): logika jest odwrócona na poziomie CheckConditions.
		// Tutaj po prostu zwracamy prawdę, czy tag istnieje.
		return hasTag;
	}

	private void PlayLines(Godot.Collections.Array linesRaw)
	{
		_currentLines.Clear();
		foreach (var l in linesRaw)
		{
			_currentLines.Add((Godot.Collections.Dictionary)l);
		}
		
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
			
			// Teksty
			if (NameLabel != null) NameLabel.Text = (string)line.GetValueOrDefault("name", "???");
			if (TextLabel != null)
			{
				TextLabel.Text = (string)line.GetValueOrDefault("text", "...");
				TextLabel.VisibleRatio = 0;
			}
			_visibleRatio = 0;

			// Portret
			if (PortraitRect != null)
			{
				if (line.ContainsKey("portrait"))
				{
					string path = PortraitsPath + (string)line["portrait"] + ".png";
					if (ResourceLoader.Exists(path))
					{
						PortraitRect.Texture = ResourceLoader.Load<Texture2D>(path);
						PortraitRect.Visible = true;
					}
					else PortraitRect.Visible = false;
				}
				else PortraitRect.Visible = false;
			}

			// Komendy
			if (line.ContainsKey("command"))
			{
				ExecuteDialogueCommand((string)line["command"]);
			}

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
	}

	private void ExecuteDialogueCommand(string commandRaw)
	{
		var parts = commandRaw.Split(':');
		string action = parts[0];

		switch (action)
		{
			case "start_quest":
				if (parts.Length >= 2) QuestManager.Instance?.StartQuest(parts[1]);
				break;
			case "progress_quest":
				if (parts.Length >= 2) 
				{
					int amt = 1;
					if (parts.Length > 2) int.TryParse(parts[2], out amt);
					QuestManager.Instance?.ProgressQuest(parts[1], amt);
				}
				break;
			case "finish_quest":
				if (parts.Length >= 2) QuestManager.Instance?.ForceCompleteQuest(parts[1]);
				break;
			case "give_item":
				if (parts.Length >= 2) NotificationUI.Instance?.AddNotification("OTRZYMANO PRZEDMIOT", parts[1]);
				break;
			case "add_tag":
				if (parts.Length >= 2) TagManager.Instance?.AddTag(parts[1]);
				break;
			case "unlock_machine":
				if (parts.Length >= 2) MainGameManager.Instance?.UnlockMachine(parts[1]);
				break;
		}
	}

	public override void _Process(double delta)
	{
		if (!_isActive) return;

		// Efekt pisania
		if (TextLabel != null && TextLabel.VisibleRatio < 1.0)
		{
			_visibleRatio += delta * 2.0; 
			TextLabel.VisibleRatio = (float)_visibleRatio;
		}

		// Pomijanie / Następna linia
		if (Input.IsActionJustPressed("ui_accept") || Input.IsActionJustPressed("interact"))
		{
			if (TextLabel != null && TextLabel.VisibleRatio < 1.0)
			{
				TextLabel.VisibleRatio = 1.0f;
				_visibleRatio = 1.0;
			}
			else
			{
				ShowNextLine();
			}
		}
	}
}
