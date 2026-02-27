using Godot;
using System;

public partial class StoryEventHandler : Node
{
	[ExportGroup("Ustawienia Fabularne")]
	[Export] public string IntroDialogueID = "intro_game";
	
	// Zmienna flagi - zmień numer (v2, v3...), jeśli chcesz zresetować intro podczas testów
	private string _introFlagName = "intro_game_finished_v1";

	[ExportGroup("Dialogi po Naprawie")]
	[Export] public string DialogMachine1 = "dialog_fix_computer";
	[Export] public string DialogMachine2 = "dialog_fix_laser";
	[Export] public string DialogMachine3 = "dialog_fix_mixer";
	[Export] public string DialogMachine4 = "dialog_fix_reactor";

	public override void _Ready()
	{
		// Czekamy klatkę, aż systemy (Manager, Singletony) wstaną
		CallDeferred(nameof(CheckStoryEvents));
		GD.Print("[Story] Skrypt StoryEventHandler załadowany do drzewa sceny!");
	}

private void CheckStoryEvents()
{
	GD.Print("[Story] Uruchamiam sprawdzanie zdarzeń..."); // Dodaj to!

	if (MainGameManager.Instance == null) 
	{
		GD.PrintErr("[Story] BŁĄD: MainGameManager.Instance jest null!");
		return;
	}
		if (MainGameManager.Instance == null) return;

		// --- 1. INTRO ---
		// Sprawdzamy, czy gracz już widział intro
		if (!MainGameManager.Instance.CheckFlag(_introFlagName))
		{
			GD.Print("[Story] Pierwsze uruchomienie - Startuję Dialog Intro.");

			if (DialogueManager.Instance != null)
			{
				// Odpalamy TYLKO dialog. 
				// To dialog (w JSON) ma na końcu komendę "show_controls", która otworzy okno.
				DialogueManager.Instance.StartDialogue(IntroDialogueID);
			}
			else
			{
				GD.PrintErr("[Story] BŁĄD: Brak DialogueManager na scenie!");
			}

			// Zapisujemy flagę, że intro się odbyło
			MainGameManager.Instance.SetFlag(_introFlagName, true);
			
			// Kończymy (nie sprawdzamy maszyn na starcie nowej gry)
			return;
		}

		// --- 2. NAGRODY ZA NAPRAWĘ MASZYN ---
		CheckAndPlay("machine_1", DialogMachine1, "m1_reward_shown");
		CheckAndPlay("machine_2", DialogMachine2, "m2_reward_shown");
		CheckAndPlay("machine_3", DialogMachine3, "m3_reward_shown");
		CheckAndPlay("machine_4", DialogMachine4, "m4_reward_shown");
	}

	private void CheckAndPlay(string machineId, string dialogueId, string flagName)
	{
		// Jeśli maszyna naprawiona ORAZ nagroda nieodebrana -> Pokaż dialog
		if (MainGameManager.Instance.IsMachineFixed(machineId) && 
			!MainGameManager.Instance.CheckFlag(flagName))
		{
			GD.Print($"[Story] Maszyna {machineId} naprawiona. Odpalam dialog.");
			if (DialogueManager.Instance != null) 
				DialogueManager.Instance.StartDialogue(dialogueId);
			
			MainGameManager.Instance.SetFlag(flagName, true);
		}
	}
}
