using Godot;
using System;
using System.Collections.Generic;

public partial class MainGameManager : Node
{
	public static MainGameManager Instance { get; private set; }
	public bool IsMale = true;
	private HashSet<string> _seenDialogues = new HashSet<string>();
	private HashSet<string> _fixedMachines = new HashSet<string>();
	
	// NOWE: Maszyny, które NPC pozwolił nam naprawić
	private HashSet<string> _unlockedMachines = new HashSet<string>();
	public event Action OnSkinChanged;
	public override void _Ready()
	{
		Instance = this;
	}

	// --- DIALOGI ---
	public bool IsDialogueSeen(string dialogueId) => _seenDialogues.Contains(dialogueId);
	public void MarkDialogueSeen(string dialogueId) { if (!_seenDialogues.Contains(dialogueId)) _seenDialogues.Add(dialogueId); }

	// --- MASZYNY (Stan naprawy) ---
	public bool IsMachineFixed(string machineId) => _fixedMachines.Contains(machineId);
	public void SetMachineFixed(string machineId) { if (!_fixedMachines.Contains(machineId)) _fixedMachines.Add(machineId); }

	// --- NOWE: MASZYNY (Dostępność / Questy) ---
	public bool IsMachineUnlocked(string machineId)
	{
		return _unlockedMachines.Contains(machineId);
	}

	public void UnlockMachine(string machineId)
	{
		if (!_unlockedMachines.Contains(machineId))
		{
			_unlockedMachines.Add(machineId);
			GD.Print($"Odblokowano dostęp do maszyny: {machineId}");
		}
	}
	public void SetGender(bool isMale)
	{
		IsMale = isMale;
		GD.Print("Zmiana płci na: " + (IsMale ? "Mężczyzna" : "Kobieta"));
		
		// Powiadom subskrybentów (czyli postać gracza)
		OnSkinChanged?.Invoke();
	}
}
