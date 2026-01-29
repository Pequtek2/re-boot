using Godot;
using System;

public partial class NPC : Area2D
{
	[Export] public string NpcID = "npc_name";
	
	// Pamiętaj, żeby przypisać AnimatedSprite2D w Inspektorze!
	[Export] public AnimatedSprite2D NpcSprite; 

	private Node2D _playerBody = null;

	public override void _Ready()
	{
		// Wykrywanie wejścia/wyjścia gracza ze strefy
		BodyEntered += (body) => 
		{ 
			if (body.Name == "Player") 
				_playerBody = body as Node2D; 
		};
		
		BodyExited += (body) => 
		{ 
			if (body.Name == "Player") 
			{
				_playerBody = null; 
				// Opcjonalnie: Po wyjściu gracza ustaw NPC prosto
				// if (NpcSprite != null) NpcSprite.Play("idle_down");
			}
		};

		// Domyślna animacja na start
		if (NpcSprite != null) 
			NpcSprite.Play("idle_down");
	}

	// --- TO JEST NOWOŚĆ: Śledzenie w czasie rzeczywistym ---
	public override void _Process(double delta)
	{
		// Jeśli gracz jest w pobliżu (_playerBody nie jest null), aktualizuj kierunek co klatkę
		if (_playerBody != null)
		{
			TurnTowardsPlayer();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Interakcja tylko odpala dialog
		if (_playerBody != null && Input.IsActionJustPressed("interact"))
		{
			DialogueManager.Instance.StartDialogue(NpcID);
		}
	}

	private void TurnTowardsPlayer()
	{
		if (NpcSprite == null || _playerBody == null) return;

		// Oblicz wektor różnicy
		Vector2 direction = _playerBody.GlobalPosition - GlobalPosition;

		// Wybór animacji na podstawie kierunku
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			// Poziomo (Lewo/Prawo)
			if (direction.X > 0)
				NpcSprite.Play("idle_right");
			else
				NpcSprite.Play("idle_left");
		}
		else
		{
			// Pionowo (Góra/Dół)
			if (direction.Y > 0)
				NpcSprite.Play("idle_down");
			else
				NpcSprite.Play("idle_up");
		}
	}
}
