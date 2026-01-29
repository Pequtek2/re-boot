using Godot;
using System;
using System.Collections.Generic;

public partial class QuestLogUI : CanvasLayer
{
	[Export] public Control JournalWindow;
	[Export] public TextureButton CloseButton;

	[Export] public Control ListView;
	[Export] public VBoxContainer QuestListContainer; // Kontener na przyciski zadań

	[Export] public Control DetailsView;
	[Export] public TextureButton BackButton;
	[Export] public Label DetailsTitle;
	[Export] public RichTextLabel DetailsDesc;
	[Export] public VBoxContainer DetailsObjectivesContainer; // Kontener na listę celów

	// Przechowujemy ID aktualnie podglądanego zadania
	private string _currentQuestId = "";

	public override void _Ready()
	{
		// Ukryj wszystko na starcie
		if (JournalWindow != null) JournalWindow.Visible = false;
		
		// Konfiguracja przycisków
		if (CloseButton != null) CloseButton.Pressed += CloseJournal;
		if (BackButton != null) BackButton.Pressed += ShowListView;

		// Subskrypcja zdarzeń
		if (QuestManager.Instance != null)
		{
			QuestManager.Instance.OnQuestsUpdated += RefreshUI;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_journal"))
		{
			ToggleJournal();
		}
		else if (@event.IsActionPressed("ui_cancel") && JournalWindow.Visible)
		{
			// Jeśli jesteśmy w szczegółach -> Wróć do listy
			if (DetailsView.Visible)
			{
				ShowListView();
				GetViewport().SetInputAsHandled();
			}
			// Jeśli jesteśmy w liście -> Zamknij dziennik
			else
			{
				CloseJournal();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	// --- NAWIGACJA ---

	private void ToggleJournal()
	{
		if (JournalWindow.Visible)
			CloseJournal();
		else
			OpenJournal();
	}

	private void OpenJournal()
	{
		JournalWindow.Visible = true;
		ShowListView(); // Zawsze otwieraj na liście
	}

	private void CloseJournal()
	{
		JournalWindow.Visible = false;
	}

	private void ShowListView()
	{
		if (ListView != null) ListView.Visible = true;
		if (DetailsView != null) DetailsView.Visible = false;
		
		_currentQuestId = ""; // Reset wyboru
		RefreshQuestList();
	}

	private void ShowDetailsView(string questId)
	{
		_currentQuestId = questId;
		
		if (ListView != null) ListView.Visible = false;
		if (DetailsView != null) DetailsView.Visible = true;

		RefreshDetails(questId);
	}

	// --- ODŚWIEŻANIE DANYCH ---

	// Główna funkcja wywoływana przez QuestManager
	private void RefreshUI()
	{
		if (!IsInstanceValid(this) || !JournalWindow.Visible) return;

		// Odśwież to, co aktualnie widać
		if (ListView.Visible)
		{
			RefreshQuestList();
		}
		else if (DetailsView.Visible && !string.IsNullOrEmpty(_currentQuestId))
		{
			RefreshDetails(_currentQuestId);
		}
	}

	private void RefreshQuestList()
	{
		// Wyczyść listę
		foreach (Node child in QuestListContainer.GetChildren()) child.QueueFree();

		var activeQuests = QuestManager.Instance.GetActiveStates();

		if (activeQuests.Count == 0)
		{
			Label emptyLbl = new Label();
			emptyLbl.Text = "Brak aktywnych zadań.";
			emptyLbl.HorizontalAlignment = HorizontalAlignment.Center;
			emptyLbl.Modulate = new Color(1, 1, 1, 0.5f);
			QuestListContainer.AddChild(emptyLbl);
			return;
		}

		// Generuj przyciski
		foreach (var state in activeQuests)
		{
			var def = QuestManager.Instance.GetDefinition(state.QuestId);
			if (def == null) continue;

			Button btn = new Button();
			btn.Text = " " + def.Title; // Spacja dla odstępu
			btn.Alignment = HorizontalAlignment.Left;
			btn.AddThemeFontSizeOverride("font_size", 18);
			
			// Stylizacja przycisku (opcjonalnie - zrób wyższy)
			btn.CustomMinimumSize = new Vector2(0, 40); 

			string qId = state.QuestId;
			btn.Pressed += () => ShowDetailsView(qId);

			QuestListContainer.AddChild(btn);
		}
	}

	private void RefreshDetails(string questId)
	{
		var def = QuestManager.Instance.GetDefinition(questId);
		// Pobierz stan (jeśli zadanie zniknęło, wróć do listy)
		var state = QuestManager.Instance.GetActiveStates().Find(x => x.QuestId == questId);

		if (state == null || def == null) 
		{
			ShowListView();
			return;
		}

		// 1. Tytuł i Opis
		DetailsTitle.Text = def.Title;
		DetailsDesc.Text = def.Description;

		// 2. Cele (Objectives)
		foreach (Node child in DetailsObjectivesContainer.GetChildren()) child.QueueFree();

		for (int i = 0; i < def.Stages.Count; i++)
		{
			var stage = def.Stages[i];
			bool isPast = i < state.CurrentStageIndex;
			bool isCurrent = i == state.CurrentStageIndex;

			Label lbl = new Label();
			
			if (isPast)
			{
				// Zrobione (Przekreślone lub na zielono)
				lbl.Text = $"[✓] {stage.Objective}";
				lbl.Modulate = Colors.Green;
			}
			else if (isCurrent)
			{
				// Aktualne
				string progress = "";
				if (stage.RequiredAmount > 1)
					progress = $" ({state.CurrentAmount}/{stage.RequiredAmount})";

				lbl.Text = $"[ ] {stage.Objective}{progress}";
				lbl.Modulate = Colors.Gold; // Wyróżnij kolor
			}
			else
			{
				// Przyszłe (Ukryte lub wyszarzone)
				// Opcja A: Pokaż jako "???"
				lbl.Text = "[ ] ???";
				lbl.Modulate = Colors.Gray;
				
				// Opcja B: Jeśli chcesz widzieć wszystko od razu, odkomentuj to:
				// lbl.Text = $"[ ] {stage.Objective}";
			}

			DetailsObjectivesContainer.AddChild(lbl);
		}
	}

	public override void _ExitTree()
	{
		if (QuestManager.Instance != null)
			QuestManager.Instance.OnQuestsUpdated -= RefreshUI;
	}
}
