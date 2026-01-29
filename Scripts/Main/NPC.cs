using Godot;
using System;

public partial class NPC : Area2D
{
	// W Inspektorze wpisujesz tylko ID, np. "kierownik" albo "inzynier"
	[Export] public string NpcID = "npc_name"; 

	private bool _playerNearby = false;

	public override void _Ready()
	{
		// Wykrywanie gracza
		BodyEntered += (body) => { if(body.Name == "Player") _playerNearby = true; };
		BodyExited += (body) => { if(body.Name == "Player") _playerNearby = false; };
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_playerNearby && Input.IsActionJustPressed("interact"))
		{
			// Cała logika decyzyjna (co powiedzieć) jest teraz w DialogueManagerze!
			DialogueManager.Instance.StartDialogue(NpcID);
		}
	}
}
