using Godot;

public partial class TloMenu : ColorRect
{
	private float _czas = 0.0f;

	public override void _Process(double delta)
	{
		_czas += (float)delta;
		QueueRedraw(); // Odświeżaj rysowanie w każdej klatce
	}

	public override void _Draw()
	{
		Vector2 rozmiar = GetViewportRect().Size;
		float odstep = 50.0f; // Odległość między liniami
		float predkosc = 20.0f; // Jak szybko się przesuwa

		// Obliczamy przesunięcie (modulo, żeby się zapętlało bez końca)
		float przesuniecie = (_czas * predkosc) % odstep;

		// Rysujemy pionowe linie
		for (float x = -odstep; x < rozmiar.X + odstep; x += odstep)
		{
			// Linie są lekko przezroczyste (0.2f na końcu to przezroczystość)
			DrawLine(new Vector2(x + przesuniecie, 0), new Vector2(x + przesuniecie, rozmiar.Y), new Color(0.0f, 1.0f, 0.5f, 0.1f), 1.0f);
		}

		// Rysujemy poziome linie
		for (float y = -odstep; y < rozmiar.Y + odstep; y += odstep)
		{
			DrawLine(new Vector2(0, y + przesuniecie), new Vector2(rozmiar.X, y + przesuniecie), new Color(0.0f, 1.0f, 0.5f, 0.1f), 1.0f);
		}
		
		// Opcjonalnie: Celownik na środku
		Vector2 srodek = rozmiar / 2;
		DrawLine(srodek - new Vector2(20, 0), srodek + new Vector2(20, 0), Colors.SpringGreen, 2.0f);
		DrawLine(srodek - new Vector2(0, 20), srodek + new Vector2(0, 20), Colors.SpringGreen, 2.0f);
	}
}
