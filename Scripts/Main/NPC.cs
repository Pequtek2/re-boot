using Godot;
using System;

public partial class NPC : Area2D
{
	[ExportGroup("Dialogues")]
	[Export] public string IntroID = "npc_intro";       
	[Export] public string WaitingID = "npc_waiting";   
	[Export] public string SuccessID = "npc_success";   
	[Export] public string NextStepID = "npc_next";     
	
	// NOWE: Co mówi NPC, gdy przyjdziesz do niego za wcześnie
	[Export] public string LockedID = "npc_busy";       

	[ExportGroup("Quest Logic")]
	[Export] public string MachineID = ""; // Maszyna, którą ten NPC zleca
	
	// NOWE: Maszyna, która MUSI być naprawiona, żeby ten NPC w ogóle z nami gadał
	[Export] public string RequiredMachineID = ""; 

	private bool _playerNearby = false;

	public override void _Ready()
	{
		BodyEntered += (body) => { if(body.Name == "Player") _playerNearby = true; };
		BodyExited += (body) => { if(body.Name == "Player") _playerNearby = false; };
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_playerNearby && Input.IsActionJustPressed("interact"))
		{
			if (MainGameManager.Instance == null) return;

			// --- NOWA LOGIKA BLOKADY (PREREQUISITE) ---
			// Jeśli NPC wymaga naprawy innej maszyny, a ona nie jest gotowa...
			if (!string.IsNullOrEmpty(RequiredMachineID))
			{
				bool prevMachineFixed = MainGameManager.Instance.IsMachineFixed(RequiredMachineID);
				if (!prevMachineFixed)
				{
					// ...to NPC nas zbywa (spławia)
					DialogueManager.Instance.StartDialogue(LockedID);
					return; // Kończymy funkcję, nie odpalamy reszty logiki
				}
			}
			// -------------------------------------------

			string finalDialogueID = IntroID;

			if (!string.IsNullOrEmpty(MachineID))
			{
				bool isFixed = MainGameManager.Instance.IsMachineFixed(MachineID);
				bool isUnlocked = MainGameManager.Instance.IsMachineUnlocked(MachineID);

				if (isFixed)
				{
					if (MainGameManager.Instance.IsDialogueSeen(SuccessID)) finalDialogueID = NextStepID;
					else
					{
						finalDialogueID = SuccessID;
						MainGameManager.Instance.MarkDialogueSeen(SuccessID);
					}
				}
				else
				{
					if (isUnlocked) finalDialogueID = WaitingID;
					else
					{
						finalDialogueID = IntroID;
						MainGameManager.Instance.UnlockMachine(MachineID);
						MainGameManager.Instance.MarkDialogueSeen(IntroID);
					}
				}
			}

			DialogueManager.Instance.StartDialogue(finalDialogueID);
		}
	}
}
