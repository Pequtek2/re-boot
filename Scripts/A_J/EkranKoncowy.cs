using Godot;
using System;
using System.Threading.Tasks;

public partial class EkranKoncowy : Control
{
	// UI i Audio
	private Label _mojLabel;
	private ColorRect _kurtyna;
	private Button _btnMenu;
	private AudioStreamPlayer _audioKlik;
	private AudioStreamPlayer _audioKoniecMusic;
	[Export] public string TargetMachineID = "machine_2";

	// Zmienne do tła
	private GradientTexture2D _tloTekstura;
	private double _czas = 0;

	// ZMIENNE DO FAJERWERKÓW
	private double _czasDoWybuchu = 0;
	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		_rng.Randomize(); // Żeby wybuchy były losowe

		// 1. GENEROWANIE TŁA (1400x780)
		GenerujTloZwyciestwa();

		// 2. PRZYPISANIE WĘZŁÓW
		_mojLabel = GetNodeOrNull<Label>("Label");
		_btnMenu = GetNodeOrNull<Button>("PrzyciskMenu");
		_kurtyna = GetNodeOrNull<ColorRect>("WarstwaKurtyny/Kurtyna");
		_audioKlik = GetNodeOrNull<AudioStreamPlayer>("audio_klikniecie");
		_audioKoniecMusic = GetNodeOrNull<AudioStreamPlayer>("Audio_Koniec_Music");

		// Uruchomienie muzyki
		if (_audioKoniecMusic != null && !_audioKoniecMusic.Playing)
		{
			_audioKoniecMusic.Play();
		}

		// Animacja odsłaniania
		if (_kurtyna != null)
		{
			_kurtyna.Visible = true;
			_kurtyna.Modulate = new Color(0, 0, 0, 1);
			var tween = GetTree().CreateTween();
			tween.TweenProperty(_kurtyna, "modulate:a", 0.0f, 0.5f);
			tween.Finished += () => _kurtyna.Visible = false;
		}

		// Animacja napisu
		if (_mojLabel != null) UruchomPodskakiwanie();

		// Obsługa przycisku
		if (_btnMenu != null) _btnMenu.Pressed += NaPrzyciskMenuPressed;
	}

	public override void _Process(double delta)
	{
		// --- 1. ANIMACJA TŁA ---
		if (_tloTekstura != null)
		{
			_czas += delta * 0.8; 
			Color zloto = new Color(1.0f, 0.84f, 0.0f);     
			Color blekit = new Color(0.0f, 0.7f, 1.0f);     
			Color jasnyRoz = new Color(1.0f, 0.4f, 0.7f);   

			float mix1 = (float)(Math.Sin(_czas) * 0.5 + 0.5);
			float mix2 = (float)(Math.Cos(_czas * 0.5) * 0.5 + 0.5);

			Color kolorGora = zloto.Lerp(blekit, mix1);
			Color kolorDol = blekit.Lerp(jasnyRoz, mix2);

			if (_tloTekstura.Gradient != null)
			{
				_tloTekstura.Gradient.Colors = new Color[] { kolorGora, kolorDol };
			}
		}

		// --- 2. GENERATOR PETARD ---
		_czasDoWybuchu -= delta;
		if (_czasDoWybuchu <= 0)
		{
			StworzWybuch();
			// Następny wybuch za 0.2 do 0.8 sekundy (losowo)
			_czasDoWybuchu = _rng.RandfRange(0.2f, 0.8f);
		}
	}

	private void StworzWybuch()
	{
		// POPRAWKA: CpuParticles2D (nie CPUParticles2D)
		CpuParticles2D wybuch = new CpuParticles2D();
		
		// Ustawienia cząsteczek
		wybuch.Emitting = false; 
		wybuch.OneShot = true;   
		wybuch.Amount = 40;      
		wybuch.Lifetime = 1.5f;  
		wybuch.Explosiveness = 1.0f; 
		
		// Wygląd iskry
		wybuch.ScaleAmountMin = 4;
		wybuch.ScaleAmountMax = 8;
		
		// Fizyka wybuchu
		wybuch.Direction = new Vector2(0, -1); 
		wybuch.Spread = 180; 
		wybuch.Gravity = new Vector2(0, 150); 
		wybuch.InitialVelocityMin = 100;
		wybuch.InitialVelocityMax = 250;

		// Losowy kolor wybuchu
		Color[] kolory = { Colors.Red, Colors.Yellow, Colors.Cyan, Colors.Magenta, Colors.Lime };
		// Używamy prostego losowania bez Extension Method
		wybuch.Color = kolory[_rng.RandiRange(0, kolory.Length - 1)];

		// Losowa pozycja na ekranie
		float posX = _rng.RandfRange(100, 1300);
		float posY = _rng.RandfRange(100, 600);
		wybuch.Position = new Vector2(posX, posY);

		// Sprzątanie po sobie
		wybuch.Finished += () => wybuch.QueueFree();

		// Dodajemy do sceny i odpalamy
		AddChild(wybuch);
		wybuch.Emitting = true;
	}

	private void GenerujTloZwyciestwa()
	{
		_tloTekstura = new GradientTexture2D();
		_tloTekstura.Width = 1400;
		_tloTekstura.Height = 780;
		_tloTekstura.Fill = GradientTexture2D.FillEnum.Linear;
		_tloTekstura.FillFrom = new Vector2(0, 0); 
		_tloTekstura.FillTo = new Vector2(1, 1);   

		Gradient grad = new Gradient();
		grad.Colors = new Color[] { Colors.Gold, Colors.Azure }; 
		_tloTekstura.Gradient = grad;

		TextureRect visualTlo = new TextureRect();
		visualTlo.Texture = _tloTekstura;
		visualTlo.Size = new Vector2(1400, 780);
		visualTlo.Position = new Vector2(0, 0); 
		
		AddChild(visualTlo);
		MoveChild(visualTlo, 0); 
		if (MainGameManager.Instance != null)
				{
				// 1. Zapisz w globalnym stanie, że maszyna jest naprawiona
				MainGameManager.Instance.SetMachineFixed(TargetMachineID);
				
				// 2. ZAKTUALIZUJ ZADANIE (Zamiast CompleteQuest)
				// Używamy ID: "quest_" + ID_maszyny
				// Cel: "Wróć do Kierownika/Inżyniera po nagrodę"
				string questID = "quest_" + TargetMachineID; 
				QuestManager.Instance.ProgressQuest("main_quest_2", 1);
				
				GD.Print($"Minigra wygrana. Maszyna: {TargetMachineID}, Quest zaktualizowany.");
			}	GD.Print($"SUKCES! Maszyna {TargetMachineID} została naprawiona.");
	}

	private async void NaPrzyciskMenuPressed()
	{
		if (_audioKlik != null) _audioKlik.Play();

		if (_kurtyna != null)
		{
			_kurtyna.Visible = true;
			var tween = GetTree().CreateTween();
			tween.TweenProperty(_kurtyna, "modulate:a", 1.0f, 0.3f);
			await ToSignal(tween, "finished");
		}
		else
		{
			await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
		}

		GetTree().ChangeSceneToFile("res://Scenes/Main/FactoryHub.tscn");
	}

	private void UruchomPodskakiwanie()
	{
		Tween tween = GetTree().CreateTween().SetLoops();
		Vector2 startowaPozycja = _mojLabel.Position;
		Vector2 pozycjaGora = startowaPozycja + new Vector2(0, -20);

		tween.TweenProperty(_mojLabel, "position", pozycjaGora, 0.6f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(_mojLabel, "position", startowaPozycja, 0.6f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
	}
}
