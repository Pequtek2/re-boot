using Godot;
using System;

public partial class VisionBlocker : Area2D
{
	private Sprite2D _sprite;
	private Tween _tween;
	
	// Zmienne do śledzenia gracza
	private Node2D _playerNode = null;
	private bool _isPlayerInside = false;

	// Margines (offset) - opcjonalnie. 
	// Pozwala wejść "trochę" za ścianę zanim zniknie.
	[Export] public float YOffset = 10.0f; 

	public override void _Ready()
	{
		// Zakładam, że Sprite jest obok (sibling) lub rodzicem. Dostosuj ścieżkę!
		// Jeśli skrypt jest na Area2D, a Sprite jest obok w rodzicu:
		_sprite = GetParent().GetNode<Sprite2D>("Sprite2D");

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		// Sprawdzamy logikę TYLKO gdy gracz jest fizycznie w pobliżu (w Area2D)
		if (_isPlayerInside && _playerNode != null)
		{
			CheckVisibility();
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body.Name == "Player" || body.IsInGroup("Player"))
		{
			_playerNode = body;
			_isPlayerInside = true;
			// Nie wywołujemy Fade tutaj, tylko w _Process!
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body == _playerNode)
		{
			_isPlayerInside = false;
			_playerNode = null;
			Fade(1.0f); // Jak wyjdzie całkiem, zawsze pokazuj ścianę
		}
	}

	private void CheckVisibility()
	{
		// --- SERCE LOGIKI 2.5D ---
		// GlobalPosition.Y to "stopy" obiektu (jeśli dobrze ustawiłeś pivoty).
		
		// Jeśli stopy gracza są WYŻEJ (mniejsze Y) niż stopy ściany (minus margines)
		// To znaczy, że gracz jest ZA ścianą.
		
		float wallY = GlobalPosition.Y; // Pozycja Y Area2D (powinna być tam gdzie stopy ściany)
		float playerY = _playerNode.GlobalPosition.Y;

		if (playerY < (wallY - YOffset))
		{
			// Gracz jest ZA ścianą -> Półprzezroczystość
			// (Sprawdzamy czy już nie jest 0.4, żeby nie resetować tweenera co klatkę)
			if (_sprite.Modulate.A > 0.45f) Fade(0.4f);
		}
		else
		{
			// Gracz jest PRZED ścianą -> Pełna widoczność
			if (_sprite.Modulate.A < 0.95f) Fade(1.0f);
		}
	}

	private void Fade(float alpha)
	{
		if (_tween != null && _tween.IsValid()) _tween.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(_sprite, "modulate:a", alpha, 0.2f);
	}
}
