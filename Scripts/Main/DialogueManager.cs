using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections; // Ważne do obsługi JSON w Godot

public partial class DialogueManager : CanvasLayer
{
	// Singleton
	public static DialogueManager Instance { get; private set; }

	[Export] public Label NameLabel;
	[Export] public RichTextLabel TextLabel;
	[Export] public TextureRect PortraitRect; // Miejsce na obrazek
	[Export] public Control DialoguePanel;    // Tło/Ramka

	[Export] public string PortraitsPath = "res://Art/Portraits/"; // Ścieżka do folderu z obrazkami

	// Zmienne wewnętrzne
	private Godot.Collections.Dictionary _dialogueData;
	private List<Dictionary> _currentDialogueLines = new List<Dictionary>();
	private int _currentLineIndex = 0;
	private bool _isDialogueActive = false;

	// Efekt pisania
	private double _visibleRatio = 0.0;
	private double _typeSpeed = 0.5;

	public override void _Ready()
	{
		Instance = this;
		LoadDialogueData();
		
		// Ukryj panel na starcie
		if (DialoguePanel != null) DialoguePanel.Visible = false;
		if (PortraitRect != null) PortraitRect.Visible = false;
	}

	private void LoadDialogueData()
	{
		string filePath = "res://Dialogue/dialogues.json";
		
		if (!FileAccess.FileExists(filePath))
		{
			GD.PrintErr($"Brak pliku dialogów: {filePath}");
			return;
		}

		using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
		string content = file.GetAsText();
		
		var json = new Json();
		var error = json.Parse(content);
		
		if (error == Error.Ok)
		{
			_dialogueData = (Godot.Collections.Dictionary)json.Data;
		}
		else
		{
			GD.PrintErr("Błąd w pliku JSON: " + json.GetErrorMessage());
		}
	}

	public void StartDialogue(string dialogueId)
	{
		if (_dialogueData == null || !_dialogueData.ContainsKey(dialogueId))
		{
			GD.PrintErr($"Nie znaleziono ID dialogu: {dialogueId}");
			return;
		}

		// Pobierz listę kwestii
		var rawLines = (Godot.Collections.Array)_dialogueData[dialogueId];
		
		_currentDialogueLines.Clear();
		foreach (var line in rawLines)
		{
			_currentDialogueLines.Add((Godot.Collections.Dictionary)line);
		}

		_currentLineIndex = 0;
		_isDialogueActive = true;
		
		if (DialoguePanel != null) DialoguePanel.Visible = true;

		ShowNextLine();
	}

	private void ShowNextLine()
	{
		if (_currentLineIndex < _currentDialogueLines.Count)
		{
			var line = _currentDialogueLines[_currentLineIndex];
			
			// 1. Ustaw teksty
			if (NameLabel != null) NameLabel.Text = (string)line.GetValueOrDefault("name", "???");
			if (TextLabel != null) TextLabel.Text = (string)line.GetValueOrDefault("text", "...");
			
			// 2. Obsługa Portretu
			if (PortraitRect != null)
			{
				if (line.ContainsKey("portrait"))
				{
					string imageName = (string)line["portrait"];
					string fullPath = $"{PortraitsPath}{imageName}.png";

					if (ResourceLoader.Exists(fullPath))
					{
						PortraitRect.Texture = ResourceLoader.Load<Texture2D>(fullPath);
						PortraitRect.Visible = true;
					}
					else
					{
						GD.PrintErr($"Nie znaleziono pliku graficznego: {fullPath}");
						PortraitRect.Visible = false;
					}
				}
				else
				{
					// Brak pola 'portrait' w JSON -> ukryj obrazek
					PortraitRect.Visible = false;
				}
			}

			// 3. Reset efektu pisania
			if (TextLabel != null) TextLabel.VisibleRatio = 0;
			_visibleRatio = 0;

			_currentLineIndex++;
		}
		else
		{
			EndDialogue();
		}
	}

	private void EndDialogue()
	{
		_isDialogueActive = false;
		if (DialoguePanel != null) DialoguePanel.Visible = false;
	}

	public override void _Process(double delta)
	{
		if (!_isDialogueActive) return;

		// Efekt pisania
		if (TextLabel != null && TextLabel.VisibleRatio < 1.0)
		{
			_visibleRatio += delta * _typeSpeed * 2.0;
			TextLabel.VisibleRatio = (float)_visibleRatio;
		}

		// Obsługa klawiszy (Spacja lub E)
		if (Input.IsActionJustPressed("ui_accept") || Input.IsActionJustPressed("interact"))
		{
			if (TextLabel != null && TextLabel.VisibleRatio < 1.0)
			{
				// Pokaż od razu całość
				TextLabel.VisibleRatio = 1.0f;
				_visibleRatio = 1.0;
			}
			else
			{
				// Następna linia
				ShowNextLine();
			}
		}
	}
}
