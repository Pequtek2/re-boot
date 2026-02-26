using Godot;
using System;

// WAŻNE: Wymuszamy ProcessMode.Always w kodzie
public partial class InstantFix : Control // Zmieniliśmy na CanvasLayer
{
	[Export] public Button FixButton;
	[Export] public Label DebugLabel; // Opcjonalnie, jeśli chcesz widzieć tekst na ekranie

	public override void _Ready()
	{
		// 1. Wymuszenie działania podczas pauzy
		ProcessMode = ProcessModeEnum.Always;
		
		GD.Print("\n--- [INSTANT FIX READY] ---");
		GD.Print("Scena InstantFix została utworzona.");
		GD.Print($"Czy gra jest zapauzowana? {GetTree().Paused}");
		GD.Print($"Mój ProcessMode: {ProcessMode}");

		if (FixButton != null)
		{
			FixButton.Pressed += OnFixPressed;
			// Wymuszamy też na przycisku
			FixButton.ProcessMode = ProcessModeEnum.Always;
			GD.Print("Przycisk FixButton znaleziony i podłączony.");
		}
		else
		{
			GD.PrintErr("BŁĄD: FixButton nie jest przypisany w Inspektorze!");
		}
	}

	public override void _Process(double delta)
	{
		// Jeśli to się nie wypisuje w konsoli, to znaczy że ProcessMode jest źle ustawiony
		// (Odkomentuj linię niżej tylko na chwilę, bo zaspamuje konsolę)
		// GD.Print("InstantFix działa (klatka renderowana)");
	}

private void OnFixPressed()
	{
		if (MainGameManager.Instance == null) return;
		string id = MainGameManager.Instance.CurrentMachineID;

		// Logika naprawy i questów (bez zmian)
		MainGameManager.Instance.SetMachineFixed(id);
		if (id == "machine_1") QuestManager.Instance.ProgressQuest("main_quest_1", 1);
		else if (id == "machine_2") QuestManager.Instance.ProgressQuest("main_quest_2", 1);
		else if (id == "machine_3") QuestManager.Instance.ProgressQuest("main_quest_3", 1);
		else if (id == "machine_4") QuestManager.Instance.ProgressQuest("main_quest_4", 1);

		// --- POWRÓT DO ŚWIATA ---
		MainGameManager.Instance.ReturnToWorld();
	}
}
