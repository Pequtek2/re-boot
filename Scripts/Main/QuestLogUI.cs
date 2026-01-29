using Godot;
using System;
using System.Collections.Generic;

public partial class QuestLogUI : CanvasLayer
{
	[Export] public Control JournalWindow;
	[Export] public TextureButton CloseButton;
	
	[Export] public Control ListView;
	[Export] public VBoxContainer QuestListContainer;
	
	// TWOJA TEKSTURA PRZYCISKU
	[Export] public Texture2D QuestButtonTexture; 

	[Export] public Control DetailsView;
	[Export] public TextureButton BackButton;
	[Export] public Label DetailsTitle;
	[Export] public RichTextLabel DetailsDesc;
	[Export] public VBoxContainer DetailsObjectivesContainer;

	private string _currentQuestId = "";

	public override void _Ready()
	{
		// Ukrywamy/Pokazujemy odpowiednie okna na starcie
		if (JournalWindow != null) JournalWindow.Visible = false;
		if (ListView != null) ListView.Visible = true;
		if (DetailsView != null) DetailsView.Visible = false;

		// Podpinamy sygnały przycisków
		if (CloseButton != null) CloseButton.Pressed += CloseJournal;
		if (BackButton != null) BackButton.Pressed += ShowListView;

		// Podpinamy się pod managera zadań
		if (QuestManager.Instance != null)
			QuestManager.Instance.OnQuestsUpdated += RefreshUI;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_journal")) 
		{
			ToggleJournal();
		}
		else if (@event.IsActionPressed("ui_cancel") && JournalWindow.Visible)
		{
			if (DetailsView.Visible) ShowListView();
			else CloseJournal();
			GetViewport().SetInputAsHandled();
		}
	}

	private void ToggleJournal()
	{
		if (JournalWindow.Visible) CloseJournal();
		else OpenJournal();
	}

	private void OpenJournal()
	{
		JournalWindow.Visible = true;
		ShowListView();
	}

	private void CloseJournal()
	{
		if (JournalWindow != null) JournalWindow.Visible = false;
	}

	private void ShowListView()
	{
		if (ListView != null) ListView.Visible = true;
		if (DetailsView != null) DetailsView.Visible = false;
		_currentQuestId = "";
		RefreshQuestList();
	}

	private void ShowDetailsView(string questId)
	{
		_currentQuestId = questId;
		if (ListView != null) ListView.Visible = false;
		if (DetailsView != null) DetailsView.Visible = true;
		RefreshDetails(questId);
	}

	private void RefreshUI()
	{
		if (!IsInstanceValid(this) || !JournalWindow.Visible) return;

		if (ListView.Visible) RefreshQuestList();
		else if (DetailsView.Visible && !string.IsNullOrEmpty(_currentQuestId)) RefreshDetails(_currentQuestId);
	}

	// --- TUTAJ SĄ GŁÓWNE ZMIANY ---
	private void RefreshQuestList()
	{
		// Odstęp między przyciskami
		QuestListContainer.AddThemeConstantOverride("separation", 10);

		// Czyścimy starą listę
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

		foreach (var state in activeQuests)
		{
			var def = QuestManager.Instance.GetDefinition(state.QuestId);
			if (def == null) continue;

			Button btn = new Button();
			btn.Text = def.Title;
			
			// 1. SZTYWNE WYMIARY
			// Zmniejszyłem do 300 (szerokość) i 45 (wysokość). 
			// Dzięki temu w oknie 400px będzie spory margines.
			btn.CustomMinimumSize = new Vector2(300, 45); 
			
			// Centrowanie przycisku w kontenerze
			btn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter; 

			// 2. ABSOLUTNA BLOKADA SCROLLOWANIA I ZAWIJANIA
			// Wyłączamy zawijanie wierszy (to blokuje rośnięcie w pionie)
			btn.AutowrapMode = TextServer.AutowrapMode.Off; 
			// Włączamy ucinanie nadmiaru
			btn.ClipText = true; 
			// Dodajemy "..." na końcu uciętego tekstu
			btn.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; 
			
			// 3. STYL TEKSTU
			btn.Alignment = HorizontalAlignment.Center; // Tekst na środku w poziomie
			btn.VerticalIconAlignment = VerticalAlignment.Center; // I w pionie
			btn.AddThemeFontSizeOverride("font_size", 16); // Mniejsza czcionka

			// 4. TEKSTURA (SKALOWANIE TŁA)
			if (QuestButtonTexture != null)
			{
				var styleBox = new StyleBoxTexture();
				styleBox.Texture = QuestButtonTexture;
				
				// Rozciągnij grafikę, żeby pasowała do wymiaru 300x45
				styleBox.AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Stretch;
				styleBox.AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Stretch;
				
				// Marginesy wewnętrzne (Padding) - żeby tekst nie dotykał krawędzi
				styleBox.ContentMarginLeft = 15;
				styleBox.ContentMarginRight = 15;
				styleBox.ContentMarginTop = 5;
				styleBox.ContentMarginBottom = 5;

				// Nadpisujemy wszystkie stany przycisku tą samą grafiką
				btn.AddThemeStyleboxOverride("normal", styleBox);
				btn.AddThemeStyleboxOverride("hover", styleBox);
				btn.AddThemeStyleboxOverride("pressed", styleBox);
				btn.AddThemeStyleboxOverride("focus", styleBox);
				btn.AddThemeStyleboxOverride("disabled", styleBox);
			}

			string qId = state.QuestId;
			btn.Pressed += () => ShowDetailsView(qId);

			QuestListContainer.AddChild(btn);
		}
	}

	private void RefreshDetails(string questId)
	{
		var def = QuestManager.Instance.GetDefinition(questId);
		var state = QuestManager.Instance.GetActiveStates().Find(x => x.QuestId == questId);

		if (state == null || def == null) 
		{
			ShowListView();
			return;
		}

		DetailsTitle.Text = def.Title;
		DetailsDesc.Text = def.Description;

		foreach (Node child in DetailsObjectivesContainer.GetChildren()) child.QueueFree();

		for (int i = 0; i < def.Stages.Count; i++)
		{
			var stage = def.Stages[i];
			bool isPast = i < state.CurrentStageIndex;
			bool isCurrent = i == state.CurrentStageIndex;

			Label lbl = new Label();
			if (isPast)
			{
				lbl.Text = $"[✓] {stage.Objective}";
				lbl.Modulate = Colors.Green;
			}
			else if (isCurrent)
			{
				string progress = stage.RequiredAmount > 1 ? $" ({state.CurrentAmount}/{stage.RequiredAmount})" : "";
				lbl.Text = $"[ ] {stage.Objective}{progress}";
				lbl.Modulate = Colors.Gold;
			}
			else
			{
				lbl.Text = "[ ] ???";
				lbl.Modulate = Colors.Gray;
			}
			DetailsObjectivesContainer.AddChild(lbl);
		}
	}
	
	public override void _ExitTree()
	{
		if (QuestManager.Instance != null) QuestManager.Instance.OnQuestsUpdated -= RefreshUI;
	}
}
