using Godot;
using System;
using System.Collections.Generic;

public partial class MainGameManager : Node
{
	public static MainGameManager Instance { get; private set; }

	// --- DANE DO ZAPISU POZYCJI ---
	public Vector2 LastPlayerPosition { get; set; } = Vector2.Zero; // Tu zapiszemy gdzie stał gracz
	public string LastScenePath { get; set; } = ""; // Tu zapiszemy nazwę poziomu (np. World.tscn)
	public bool ShouldTeleportPlayer { get; set; } = false; // Flaga: "Czy mam teleportować gracza po załadowaniu?"

	// --- DANE DO MINIGIER ---
	public string CurrentMachineID { get; set; } = "";

	// --- DANE GRY (Questy, Maszyny) ---
	public bool IsMale = true;
	public event Action OnSkinChanged;
	private HashSet<string> _seenDialogues = new HashSet<string>();
	private HashSet<string> _fixedMachines = new HashSet<string>();
	private HashSet<string> _unlockedMachines = new HashSet<string>();

	public override void _Ready()
	{
		Instance = this;
	}

	// --- 1. PRZEJŚCIE DO MINIGRY ---
	public void SwitchToMinigame(string machineId, string minigamePath, Vector2 playerPos, string currentScenePath)
	{
		// 1. Zapamiętaj stan
		CurrentMachineID = machineId;
		LastPlayerPosition = playerPos;
		LastScenePath = currentScenePath;
		ShouldTeleportPlayer = true; // Ważne: Włączamy flagę teleportacji

		GD.Print($"[Manager] Zapisano pozycję: {playerPos}. Zmieniam scenę na: {minigamePath}");

		// 2. Zmień scenę (Stara scena zostaje usunięta z pamięci!)
		GetTree().ChangeSceneToFile(minigamePath);
	}

	// --- 2. POWRÓT Z MINIGRY ---
	public void ReturnToWorld()
	{
		if (string.IsNullOrEmpty(LastScenePath))
		{
			GD.PrintErr("Błąd: Nie wiem do jakiej sceny wrócić!");
			return;
		}

		GD.Print($"[Manager] Wracam do świata: {LastScenePath}");
		GetTree().ChangeSceneToFile(LastScenePath);
	}

	// --- RESZTA LOGIKI (Bez zmian) ---
	public bool IsDialogueSeen(string dialogueId) => _seenDialogues.Contains(dialogueId);
	public void MarkDialogueSeen(string dialogueId) { if (!_seenDialogues.Contains(dialogueId)) _seenDialogues.Add(dialogueId); }
	public bool IsMachineFixed(string machineId) => _fixedMachines.Contains(machineId);
	public void SetMachineFixed(string machineId) { if (!_fixedMachines.Contains(machineId)) _fixedMachines.Add(machineId); }
	public bool IsMachineUnlocked(string machineId) => _unlockedMachines.Contains(machineId);
	public void UnlockMachine(string machineId) { if (!_unlockedMachines.Contains(machineId)) _unlockedMachines.Add(machineId); }
	public void SetGender(bool isMale) { IsMale = isMale; OnSkinChanged?.Invoke(); }
}
