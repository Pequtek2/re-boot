using Godot;
using System.Collections.Generic;

public partial class MenuSkrypt : Control
{
	private List<Control> _elementyDeszczu = new List<Control>();
	private float _szerokosc;
	private float _wysokosc;

	private Label _srubkaLabel;
	private float _oryginalnaPozycjaY;
	private float _czas = 0;

	public override void _Ready()
	{
		_szerokosc = GetViewportRect().Size.X;
		_wysokosc = GetViewportRect().Size.Y;

		_srubkaLabel = GetNodeOrNull<Label>("Srubka_Label");
		if (_srubkaLabel != null)
		{
			_oryginalnaPozycjaY = _srubkaLabel.Position.Y;
			// Ważne: ustawiamy pivot na środek, żeby rotacja i skala wyglądały dobrze
			_srubkaLabel.PivotOffset = _srubkaLabel.Size / 2;
		}

		ColorRect tloWizualne = GetNodeOrNull<ColorRect>("Tlo");
		if (tloWizualne != null)
		{
			tloWizualne.Color = new Color(0.02f, 0.0f, 0.05f, 1.0f);
		}

		for (int i = 0; i < 100; i++)
		{
			StworzElementDeszczu(true);
		}

		Button btnStart = GetNodeOrNull<Button>("VBoxContainer/Button");
		Button btnExit = GetNodeOrNull<Button>("VBoxContainer/Button2");

		if (btnStart != null) btnStart.Pressed += () => GetTree().ChangeSceneToFile("res://Scenes/A_J/MiniGameA_J.tscn");
		if (btnExit != null) btnExit.Pressed += () => GetTree().Quit();
	}

	public override void _Process(double delta)
{
	_czas += (float)delta;

	// --- SUBTELNE PŁYWANIE NAPISU ---
	if (_srubkaLabel != null)
	{
		// 2.0f - to prędkość (im mniejsza, tym wolniej płynie)
		// 6.0f - to wysokość (tylko 6 pikseli góra-dół, czyli bardzo delikatnie)
		float przesuniecie = Mathf.Sin(_czas * 4.0f) * 6.0f;
		
		_srubkaLabel.Position = new Vector2(_srubkaLabel.Position.X, _oryginalnaPozycjaY + przesuniecie);
		
		// Resetujemy rotację i kolory na wypadek, gdyby został tamten glitch
		_srubkaLabel.Rotation = 0;
		_srubkaLabel.Modulate = new Color(1, 1, 1);
	}

	// --- OBSŁUGA DESZCZU (BEZ ZMIAN) ---
	for (int i = _elementyDeszczu.Count - 1; i >= 0; i--)
	{
		Control el = _elementyDeszczu[i];
		float predkosc = (float)el.GetMeta("v");
		el.Position += new Vector2(0, predkosc * (float)delta);

		if (el.Position.Y > _wysokosc + 50)
		{
			el.Position = new Vector2(GD.Randf() * _szerokosc, -60);
		}
	}
}
	private void StworzElementDeszczu(bool losowyStart)
	{
		Control nowyElement;
		if (GD.Randf() < 0.3f)
		{
			Label label = new Label();
			label.Text = "?";
			label.AddThemeFontSizeOverride("font_size", GD.RandRange(30, 50));
			nowyElement = label;
		}
		else
		{
			ColorRect pasek = new ColorRect();
			pasek.Size = new Vector2(GD.RandRange(2, 4), GD.RandRange(20, 60));
			nowyElement = pasek;
		}

		float startX = GD.Randf() * _szerokosc;
		float startY = losowyStart ? GD.Randf() * _wysokosc : -60;
		nowyElement.Position = new Vector2(startX, startY);

		Color kolor;
		float los = GD.Randf();
		if (los < 0.33f) kolor = new Color(0.6f, 0.0f, 1.0f);
		else if (los < 0.66f) kolor = new Color(1.0f, 0.2f, 0.8f);
		else kolor = new Color(0.0f, 0.8f, 1.0f);
		
		kolor.A = (float)GD.RandRange(0.4f, 0.9f);
		nowyElement.Modulate = kolor;
		nowyElement.SetMeta("v", (float)GD.RandRange(100, 350));

		AddChild(nowyElement);
		MoveChild(nowyElement, 1);
		_elementyDeszczu.Add(nowyElement);
	}
}
