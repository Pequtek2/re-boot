using Godot;
using System;

public partial class NPC : Area2D
{
	[ExportGroup("Ustawienia NPC")]
	[Export] public string NpcID = "npc_name";
	[Export] public AnimatedSprite2D NpcSprite; 

	[ExportGroup("Ikonka Interakcji")]
	[Export] public Sprite2D InteractionIcon; // Przeciągnij tutaj swoją ikonkę "E"
	[Export] public float FloatAmplitude = 3.0f; // Jak wysoko ma skakać ikonka
	[Export] public float FloatSpeed = 4.0f;     // Jak szybko ma lewitować

	private Node2D _playerBody = null;
	private float _originalIconY;
	private double _timePassed = 0.0;

	public override void _Ready()
	{
		// Zapamiętujemy startową pozycję ikonki
		if (InteractionIcon != null)
		{
			_originalIconY = InteractionIcon.Position.Y;
			InteractionIcon.Visible = false; // Ukrywamy na start
		}

		BodyEntered += (body) => 
		{ 
			if (body.Name == "Player" || body.IsInGroup("Player")) 
			{
				_playerBody = body as Node2D; 
				if (InteractionIcon != null) InteractionIcon.Visible = true; // Pokazujemy ikonkę
			}
		};
		
		BodyExited += (body) => 
		{ 
			if (body.Name == "Player" || body.IsInGroup("Player")) 
			{
				_playerBody = null; 
				if (InteractionIcon != null) InteractionIcon.Visible = false; // Ukrywamy ikonkę
			}
		};

		if (NpcSprite != null) 
			NpcSprite.Play("idle_down");
	}

	public override void _Process(double delta)
	{
		if (_playerBody != null)
		{
			TurnTowardsPlayer(); // NPC obraca się w stronę gracza

			// Animacja lewitowania ikonki
			if (InteractionIcon != null && InteractionIcon.Visible)
			{
				_timePassed += delta;
				float newY = _originalIconY + Mathf.Sin((float)_timePassed * FloatSpeed) * FloatAmplitude;
				InteractionIcon.Position = new Vector2(InteractionIcon.Position.X, newY);
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_playerBody != null && Input.IsActionJustPressed("interact"))
		{
			if (DialogueManager.Instance != null)
				DialogueManager.Instance.StartDialogue(NpcID); // Uruchomienie dialogu
		}
	}

	private void TurnTowardsPlayer()
	{
		if (NpcSprite == null || _playerBody == null) return;

		Vector2 direction = _playerBody.GlobalPosition - GlobalPosition;
		
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			NpcSprite.Play(direction.X > 0 ? "idle_right" : "idle_left");
		}
		else
		{
			NpcSprite.Play(direction.Y > 0 ? "idle_down" : "idle_up");
		}
	}
}
