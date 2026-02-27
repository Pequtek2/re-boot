using Godot;
using System;

public partial class ObjectiveMarker : Sprite2D
{
	[ExportGroup("Tag Settings")]
	[Export] public string RequiredTag = ""; // Tag, który MUSI być, by wykrzyknik się pokazał (zostaw puste, jeśli ma być od początku)
	[Export] public string HidingTag = "";   // Tag, po którym wykrzyknik ZNIKA (np. po rozmowie z NPC lub naprawie)

	[ExportGroup("Animation")]
	[Export] public float FloatAmplitude = 5.0f; // Wysokość skakania
	[Export] public float FloatSpeed = 4.0f;     // Prędkość skakania

	private float _originalY;
	private double _timePassed = 0.0;

	public override void _Ready()
	{
		// Zapamiętujemy pozycję startową do animacji lewitowania
		_originalY = Position.Y;
	}

	public override void _Process(double delta)
	{
		// 1. Opcjonalna animacja lewitowania
		_timePassed += delta;
		Position = new Vector2(Position.X, _originalY + Mathf.Sin((float)_timePassed * FloatSpeed) * FloatAmplitude);

		// 2. Sprawdzanie widoczności na podstawie tagów
		CheckVisibility();
	}

	private void CheckVisibility()
	{
		// Zabezpieczenie, upewnij się, że Twój TagManager ma instancję!
		if (TagManager.Instance == null) 
		{
			Visible = false;
			return;
		}

		// UWAGA: Założyłem, że w TagManager masz metodę HasTag. 
		// Jeśli nazywa się inaczej (np. IsTagActive), podmień to poniżej:
		
		bool hasRequired = string.IsNullOrEmpty(RequiredTag) || TagManager.Instance.HasTag(RequiredTag);
		bool hasHiding = !string.IsNullOrEmpty(HidingTag) && TagManager.Instance.HasTag(HidingTag);

		// Wykrzyknik jest widoczny tylko wtedy, gdy mamy tag wymagany i NIE mamy tagu ukrywającego
		Visible = hasRequired && !hasHiding;
	}
}
