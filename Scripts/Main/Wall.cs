using Godot;

public partial class Wall : Node2D
{
	private Sprite2D _sprite;
	private Tween _tween;

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite2D"); // Dopasuj ścieżkę do swojego Sprite'a
		AddToGroup("Occluders"); // Dodajemy do grupy, żeby Raycast nas znalazł
	}

	// Ta funkcja będzie wołana przez Kamerę
	public void SetTransparent(bool transparent)
	{
		float targetAlpha = transparent ? 0.4f : 1.0f; // 0.4 = przezroczysty, 1.0 = widoczny

		// Jeśli już jesteśmy w trakcie zmiany na ten sam kolor, nie rób nic
		if (_sprite.Modulate.A == targetAlpha) return;

		// Resetujemy stary tween
		if (_tween != null && _tween.IsValid()) _tween.Kill();

		_tween = CreateTween();
		_tween.TweenProperty(_sprite, "modulate:a", targetAlpha, 0.2f); // 0.2s animacji
	}
}
