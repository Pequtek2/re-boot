using Godot;
using System;
using System.Threading.Tasks;

public partial class Transitioner : CanvasLayer
{
	private TextureProgressBar _progress;
	private ColorRect _bg;
	private Label _statusLabel;
	private ColorRect _cctvOverlay; 
	private Random _rnd = new Random();

	private string[] _bootTexts = {
		"BIOS: OK", "RAM TEST: OK", "LOADING KERNEL...",
		"INITIALIZING HACK_OS...", "SYSTEM ONLINE."
	};

	public override void _Ready()
	{
		// Przypisanie węzłów zgodnie z Twoim zdjęciem hierarchii
		_bg = GetNodeOrNull<ColorRect>("ColorRect");
		_progress = GetNodeOrNull<TextureProgressBar>("TextureProgressBar");
		_statusLabel = GetNodeOrNull<Label>("StatusLabel");
		
		// Szukamy nakładki wewnątrz dodatkowego CanvasLayer
		_cctvOverlay = GetNodeOrNull<ColorRect>("CanvasLayer/CCTVnakldaka"); 

		Visible = false;
		if (_bg != null) _bg.Modulate = new Color(1, 1, 1, 0);
		
		UstawGlitch(0.1f);
	}

	public async Task ChangeScene(string targetScenePath, bool isInitialBoot = false)
	{
		if (!IsInsideTree()) return;

		Visible = true;
		if (_progress != null) _progress.Value = 0;
		if (_statusLabel != null) _statusLabel.Text = "";

		// 1. Start: Mocny glitch i zaciemnienie
		UstawGlitch(0.8f);
		if (_bg != null)
		{
			Tween fadeIn = CreateTween();
			fadeIn.TweenProperty(_bg, "modulate:a", 1.0f, 0.4f);
			await ToSignal(fadeIn, Tween.SignalName.Finished);
		}

		// 2. Ładowanie
		double currentProgress = 0;
		int textIndex = 0;

		while (currentProgress < 100)
		{
			if (!IsInsideTree()) return;
			
			currentProgress += _rnd.Next(2, 7); 
			if (currentProgress > 100) currentProgress = 100;

			if (_progress != null) _progress.Value = currentProgress;

			if (isInitialBoot && _statusLabel != null && textIndex < _bootTexts.Length)
			{
				if (currentProgress > (100.0 / _bootTexts.Length) * (textIndex + 1))
				{
					_statusLabel.Text = _bootTexts[textIndex];
					textIndex++;
				}
			}

			await ToSignal(GetTree().CreateTimer(0.1f), SceneTreeTimer.SignalName.Timeout);
		}

		await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

		// 3. Zmiana sceny (sprawdź czy plik istnieje w konsoli jeśli wywali błąd)
		if (IsInsideTree()) GetTree().ChangeSceneToFile(targetScenePath);

		// 4. Koniec: Słaby glitch i rozjaśnienie
		UstawGlitch(0.1f);
		if (_bg != null)
		{
			Tween fadeOut = CreateTween();
			fadeOut.TweenProperty(_bg, "modulate:a", 0.0f, 0.4f);
			await ToSignal(fadeOut, Tween.SignalName.Finished);
		}

		Visible = false;
	}

	private void UstawGlitch(float rate)
	{
		// Sprawdzamy czy materiał to ShaderMaterial i czy parametr się zgadza
		if (_cctvOverlay?.Material is ShaderMaterial mat)
		{
			mat.SetShaderParameter("shake_rate", rate);
		}
	}
}
