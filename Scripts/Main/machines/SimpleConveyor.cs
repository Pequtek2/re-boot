using Godot;
using System;
using System.Collections.Generic; // Potrzebne do List<>

public partial class SimpleConveyor : Node2D
{
	[Export] public float Speed = 30.0f; // Prędkość przesuwania
	
	// Kierunek w 2.5D.
	// Dla taśmy South-East (prawa-dół) ustaw: X=2, Y=1
	// Dla taśmy North-East (prawa-góra) ustaw: X=2, Y=-1
	[Export] public Vector2 MoveDirection = new Vector2(2, 1); 
	
	[Export] public float ResetDistance = 32.0f; // Po ilu pikselach ruda wraca na start?

	[Export] public Sprite2D ItemSprite;
	[Export] public AnimatedSprite2D BeltAnim;
	
	// Lista rud do losowania (wrzuć tu Copper, Iron, Coal w inspektorze)
	[Export] public Texture2D[] PossibleItems;

	private Vector2 _startPos;

	public override void _Ready()
	{
		if (ItemSprite != null)
		{
			_startPos = ItemSprite.Position;
			RandomizeOre(); // Losuj wygląd na starcie
		}

		// Uruchom animację taśmy
		if (BeltAnim != null) BeltAnim.Play("working");
	}

	public override void _Process(double delta)
	{
		if (ItemSprite == null) return;

		// 1. Przesuwanie
		// Normalized() jest ważne, żeby ruch po skosie nie był szybszy
		ItemSprite.Position += MoveDirection.Normalized() * Speed * (float)delta;

		// 2. Sprawdzanie dystansu (Pętla)
		float distanceTraveled = ItemSprite.Position.DistanceTo(_startPos);
		
		if (distanceTraveled >= ResetDistance)
		{
			ResetItem();
		}
	}

	private void ResetItem()
	{
		// Wróć na początek
		ItemSprite.Position = _startPos;
		
		// Opcjonalnie: Zmień rudę na inną przy każdym cyklu (daje fajny efekt różnorodności)
		// RandomizeOre(); 
	}

	private void RandomizeOre()
	{
		if (PossibleItems == null || PossibleItems.Length == 0) return;

		// Wybierz losową teksturę z tablicy
		var randomTexture = PossibleItems[GD.Randi() % PossibleItems.Length];
		ItemSprite.Texture = randomTexture;
	}
}
