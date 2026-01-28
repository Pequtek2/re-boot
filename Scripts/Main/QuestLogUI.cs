using Godot;
using System;
using System.Collections.Generic; // Potrzebne do List<>

public partial class QuestLogUI : CanvasLayer
{
	// Musimy przypisać te elementy w Inspektorze
	[Export] public Control JournalWindow;
	[Export] public VBoxContainer QuestListContainer;

	public override void _Ready()
	{
		// Ukryj na starcie
		if (JournalWindow != null) 
			JournalWindow.Visible = false;

		// Jeśli QuestManager istnieje, subskrybuj zmiany
		if (QuestManager.Instance != null)
		{
			QuestManager.Instance.OnQuestsUpdated += RefreshUI;
		}
	}

public override void _UnhandledInput(InputEvent @event)
{
	// 1. Otwieranie/Zamykanie na "J" (toggle_journal)
	if (@event.IsActionPressed("toggle_journal"))
	{
		ToggleJournal();
	}
	// 2. Zamykanie na "ESC" (ui_cancel)
	else if (@event.IsActionPressed("ui_cancel"))
	{
		// Sprawdzamy, czy okno jest widoczne (żeby nie "zamykać" zamkniętego okna)
		if (JournalWindow != null && JournalWindow.Visible)
		{
			ToggleJournal(); // Zamknij okno
			
			// Ważne: Oznaczamy wejście jako "obsłużone". 
			// Dzięki temu ESC nie otworzy Menu Pauzy w tym samym momencie.
			GetViewport().SetInputAsHandled(); 
		}
	}
}

	private void ToggleJournal()
	{
		if (JournalWindow == null) return;

		JournalWindow.Visible = !JournalWindow.Visible;

		// Jeśli właśnie otworzyliśmy, odświeżmy listę (dla pewności)
		if (JournalWindow.Visible)
		{
			RefreshUI();
		}
	}

	private void RefreshUI()
	{
		if (QuestListContainer == null || QuestManager.Instance == null) return;

		// 1. Wyczyść stare wpisy (usuwamy dzieci VBoxa)
		foreach (Node child in QuestListContainer.GetChildren())
		{
			child.QueueFree();
		}

		// 2. Pobierz aktywne zadania
		var activeQuests = QuestManager.Instance.GetActiveQuests();

		if (activeQuests.Count == 0)
		{
			Label emptyLbl = new Label();
			emptyLbl.Text = "Brak aktywnych zadań.";
			emptyLbl.Modulate = new Color(1, 1, 1, 0.5f); // Szary
			QuestListContainer.AddChild(emptyLbl);
			return;
		}

		// 3. Stwórz wpisy dla każdego zadania
		foreach (var quest in activeQuests)
		{
			// Kontener na pojedyncze zadanie
			VBoxContainer entry = new VBoxContainer();
			
			// Tytuł zadania (np. złoty kolor)
			Label titleLbl = new Label();
			titleLbl.Text = "• " + quest.Title;
			titleLbl.Modulate = Colors.Gold; 
			titleLbl.AddThemeFontSizeOverride("font_size", 18);
			
			// Cel zadania (biały, mniejszy)
			Label objectiveLbl = new Label();
			objectiveLbl.Text = "   > " + quest.CurrentObjective;
			objectiveLbl.Modulate = Colors.LightGray;
			
			entry.AddChild(titleLbl);
			entry.AddChild(objectiveLbl);
			
			// Dodaj odstęp (HSeparator - niewidzialny lub linia)
			entry.AddChild(new HSeparator());

			QuestListContainer.AddChild(entry);
		}
	}
	
	// Pamiętaj o odsubskrybowaniu przy niszczeniu obiektu, by uniknąć błędów
	public override void _ExitTree()
	{
		if (QuestManager.Instance != null)
		{
			QuestManager.Instance.OnQuestsUpdated -= RefreshUI;
		}
	}
}
