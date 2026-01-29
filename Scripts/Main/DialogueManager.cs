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
		string path = "res://Dialogue/dialogues.json";
		if (!FileAccess.FileExists(path)) return;

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var json = new Json();
		if (json.Parse(file.GetAsText()) == Error.Ok)
		{
			var rawData = (Godot.Collections.Dictionary)json.Data;
			_dialogueDatabase = new Dictionary<string, Godot.Collections.Array>();

			foreach (var keyVariant in rawData.Keys)
			{
				string key = (string)keyVariant;
				_dialogueDatabase.Add(key, (Godot.Collections.Array)rawData[key]);
			}
		}
	}

	public void StartDialogue(string npcId)
	{
		if (_dialogueDatabase == null || !_dialogueDatabase.ContainsKey(npcId)) return;

		_currentNpcId = npcId; // Zapamiętaj z kim gadamy

		// --- AUTOMATYCZNE ŁADOWANIE GŁÓWNEGO PORTRETU ---
		// Próbuje załadować np. "marek.png"
		SetPortrait(npcId);
		// ------------------------------------------------

		var variants = _dialogueDatabase[npcId];
		Godot.Collections.Dictionary bestMatch = null;

		foreach (var variantObj in variants)
		{
			var variant = (Godot.Collections.Dictionary)variantObj;
			if (CheckConditions(variant))
			{
				bestMatch = variant;
				break;
			}
		}

		if (bestMatch != null)
			PlayLines((Godot.Collections.Array)bestMatch["lines"]);
		else
			PlayDefaultMessage(npcId);
	}

	private void SetPortrait(string imageName)
	{
		if (PortraitRect == null) return;

		string fullPath = PortraitsPath + imageName + ".png";
		if (ResourceLoader.Exists(fullPath))
		{
			PortraitRect.Texture = ResourceLoader.Load<Texture2D>(fullPath);
			PortraitRect.Visible = true;
			// Reset pozycji przy zmianie obrazka
			PortraitRect.Position = _portraitBasePos; 
		}
		else
		{
			// Jeśli nie ma pliku, po prostu ukryj (chyba że chcesz zostawić stary)
			 // PortraitRect.Visible = false; 
		}
	}

	private bool CheckConditions(Godot.Collections.Dictionary variant)
	{
		// Logika warunków (bez zmian)
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

			// --- OBSŁUGA PORTRETU (NADPSYWANIE) ---
			// Domyślnie mamy obrazek załadowany w StartDialogue (np. marek.png).
			// Ale jeśli w tej konkretnej linijce JSON jest "portrait": "marek_angry", to zmieniamy.
			if (line.ContainsKey("portrait"))
			{
				SetPortrait((string)line["portrait"]);
			}
			else
			{
				// Opcjonalnie: Jeśli linijka nie ma portretu, przywróć domyślny dla tego NPC
				// (Odkomentuj poniższe, jeśli chcesz, żeby emocje znikały w kolejnym zdaniu)
				// SetPortrait(_currentNpcId); 
			}
			// ---------------------------------------

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
	}

	private void ExecuteDialogueCommand(string commandRaw)
	{
		var parts = commandRaw.Split(':');
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
			case "unlock_machine": if (parts.Length >= 2) MainGameManager.Instance?.UnlockMachine(parts[1]); break;
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
				// Używamy Sinusa czasu, żeby robić góra-dół
				// Mathf.Abs sprawia, że skacze tylko do góry (jak piłeczka), a nie góra-dół
				float bounce = Mathf.Abs(Mathf.Sin(Time.GetTicksMsec() / 100.0f * (BounceSpeed / 10.0f))) * BounceHeight;
				
				// Odejmujemy od Y, bo w Godot "w górę" to minus Y
				PortraitRect.Position = new Vector2(_portraitBasePos.X, _portraitBasePos.Y - bounce);
			}
			else
			{
				// Jeśli przestał mówić, wróć na miejsce
				PortraitRect.Position = _portraitBasePos; 
			}
		}
		// -----------------------------

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
