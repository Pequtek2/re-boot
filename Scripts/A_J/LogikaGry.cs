using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public partial class LogikaGry : Node2D
{
	private List<Vector2> _wszystkiePunkty = new List<Vector2>();
	private Vector2 _ostatniPunkt = Vector2.Zero;
	private LineEdit _wejscie;
	private Camera2D _kamera;
	private Vector2 _srodek;
	
	// UI
	private CanvasLayer _panelWygranej;
	private CanvasLayer _panelInstrukcji;
	private CanvasLayer _panelFabuly;
	private ColorRect _kurtyna; 
	private Label _labelZycia; 
	
	// PRZYCISKI
	private Button _btnDalej;      
	private Button _btnStart; 
	private Button _btnMisja; 
	private Label _napisWygranej;
	private Label _napisFabuly; 
	
	// AUDIO
	private AudioStreamPlayer _audioMenuMusic; 
	private AudioStreamPlayer _audioSilnik;    
	private AudioStreamPlayer _audioBieg;      
	private AudioStreamPlayer _audioKlik;      
	private AudioStreamPlayer _audioWin;
	private AudioStreamPlayer _audioWybuch; 
	private AudioStreamPlayer _audioFail; 
	
	// ZMIENNE ROZGRYWKI
	private bool _czyPrzesuwa = false;
	private float _czuloscZoomu = 0.1f;
	private int _liczbaZyc = 5; 
	private int _maxZyc = 5;

	private List<List<Vector2>> _wszystkieZadania = new List<List<Vector2>>();
	private List<string> _nazwyObrazkow = new List<string> { "T_A_J", "Sruba_2", "Sruba_3" }; 
	private int _aktualneZadanieIndex = 0;
	private int _ktoryPunktWzoru = 1; 

	private float _skalaX = 2.75f; 
	private float _skalaY = 2.7f; 

	public override void _Ready()
	{
		_srodek = GetViewportRect().Size / 2;
		_wejscie = GetNode<LineEdit>("CanvasLayer/LineEdit");
		_kamera = GetNode<Camera2D>("Camera2D");
		
		_panelWygranej = GetNode<CanvasLayer>("WarstwaWygranej");
		_panelInstrukcji = GetNode<CanvasLayer>("CanvasLayer2");
		_btnDalej = GetNode<Button>("WarstwaWygranej/Panel/PrzyciskDalej");
		_napisWygranej = GetNode<Label>("WarstwaWygranej/Panel/Label");
		_btnStart = GetNode<Button>("CanvasLayer2/Panel/Button");

		// SZUKAMY LICZNIKA ŻYĆ
		_labelZycia = GetNodeOrNull<Label>("CanvasLayer5/LabelZycia");
		
		if (_labelZycia != null) 
		{
			_labelZycia.Visible = false; // Ukryty na start
			_labelZycia.Text = $"Pozostało prób: {_liczbaZyc}/{_maxZyc}";
		}
		else
		{
			GD.PrintErr("CRITICAL ERROR: Nie widzę LabelZycia w CanvasLayer5!");
		}

		// AUDIO
		_audioMenuMusic = GetNodeOrNull<AudioStreamPlayer>("Audio_MenuMusic");
		_audioSilnik = GetNodeOrNull<AudioStreamPlayer>("Audio_Silnik");
		_audioBieg = GetNodeOrNull<AudioStreamPlayer>("Audio_Bieg");
		_audioKlik = GetNodeOrNull<AudioStreamPlayer>("Audio_Klik");
		_audioWin = GetNodeOrNull<AudioStreamPlayer>("Audio_Win");
		_audioWybuch = GetNodeOrNull<AudioStreamPlayer>("Audio_Wybuch");
		_audioFail = GetNodeOrNull<AudioStreamPlayer>("Audio_Fail");

		// KURTYNA
		_kurtyna = GetNodeOrNull<ColorRect>("CanvasLayer4/Kurtyna"); 
		if (_kurtyna != null)
		{
			_kurtyna.Visible = true;
			_kurtyna.Modulate = new Color(0, 0, 0, 0); 
			_kurtyna.Color = Colors.Black;
			_kurtyna.MouseFilter = Control.MouseFilterEnum.Ignore; 
		}

		// FABUŁA
		_panelFabuly = GetNodeOrNull<CanvasLayer>("WarstwaFabuly");
		if (_panelFabuly != null)
		{
			_btnMisja = _panelFabuly.GetNodeOrNull<Button>("Panel/PrzyciskMisji");
			if (_btnMisja == null) _btnMisja = _panelFabuly.GetNodeOrNull<Button>("PrzyciskMisji");
			
			_napisFabuly = _panelFabuly.GetNodeOrNull<Label>("Panel/Label");
			if (_napisFabuly == null) _napisFabuly = _panelFabuly.GetNodeOrNull<Label>("Label");
			
			if (_napisFabuly != null)
			{
				var tweenNapis = CreateTween();
				tweenNapis.SetLoops(); 
				float startY = _napisFabuly.Position.Y;
				tweenNapis.TweenProperty(_napisFabuly, "position:y", startY - 10, 0.8f)
					.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
				tweenNapis.TweenProperty(_napisFabuly, "position:y", startY + 10, 0.8f)
					.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
			}

			_panelFabuly.Visible = true;
			_panelInstrukcji.Visible = false;
			if (_btnMisja != null) _btnMisja.FocusMode = Control.FocusModeEnum.None;

			if (_audioSilnik != null) _audioSilnik.Play();
		}
		else
		{
			_panelInstrukcji.Visible = true;
		}
		
		_kamera.Position = _srodek;
		_kamera.Zoom = Vector2.One;
		_panelWygranej.Visible = false;
		_wejscie.Editable = false; 

		_btnDalej.FocusMode = Control.FocusModeEnum.None; 
		_btnStart.FocusMode = Control.FocusModeEnum.None;

		// ZADANIA
		_wszystkieZadania.Add(new List<Vector2> { new Vector2(0, 0), new Vector2(20, 0), new Vector2(20, -80), new Vector2(50, -80), new Vector2(50, -100), new Vector2(-30, -100), new Vector2(-30, -80), new Vector2(0, -80), new Vector2(0, 0) });
		_wszystkieZadania.Add(new List<Vector2> { new Vector2(0, 0), new Vector2(40, 0), new Vector2(40, -40), new Vector2(0, -40), new Vector2(0, 0) });
		_wszystkieZadania.Add(new List<Vector2> { new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, -20), new Vector2(0, -20), new Vector2(0, 0) });

		_wejscie.TextSubmitted += NaWpisanieTekstu;
		
		// PRZYCISK "DALEJ" (KLIKNIĘCIE)
		_btnDalej.Pressed += async () => {
			if (_audioKlik != null) _audioKlik.Play();
			await ToSignal(GetTree().CreateTimer(0.15f), "timeout");
			PrzejdzDoNastepnejSruby();
		};

		// ==========================================================
		// 1. KLIKNIĘCIE "JUŻ LECĘ" (FABUŁA -> INSTRUKCJA)
		// ==========================================================
		if (_btnMisja != null)
		{
			_btnMisja.Pressed += async () => {
				
				// --- ZMIANA: TERAZ TEŻ JEST KLIKNIĘCIE ZAMIAST BIEGU ---
				if (_audioKlik != null) _audioKlik.Play();
				
				if (_kurtyna != null)
				{
					_kurtyna.Color = Colors.Black; 
					var tween = GetTree().CreateTween();
					tween.TweenProperty(_kurtyna, "modulate:a", 1.0f, 1.0f); 
					await ToSignal(tween, "finished");
					
					if (_panelFabuly != null) _panelFabuly.Visible = false;
					_panelInstrukcji.Visible = true;
					
					var tweenOut = GetTree().CreateTween();
					tweenOut.TweenProperty(_kurtyna, "modulate:a", 0.0f, 1.0f); 
					await ToSignal(tweenOut, "finished");
				}
				else
				{
					if (_panelFabuly != null) _panelFabuly.Visible = false;
					_panelInstrukcji.Visible = true;
				}
			};
		}

		// ==========================================================
		// 2. KLIKNIĘCIE "ZACZYNAMY" (INSTRUKCJA -> GRA)
		// ==========================================================
		_btnStart.Pressed += async () => {
			
			if (_audioKlik != null) _audioKlik.Play();
			if (_audioMenuMusic != null) _audioMenuMusic.Stop();

			if (_kurtyna != null)
			{
				_kurtyna.Color = Colors.Black; 
				var tween = GetTree().CreateTween();
				tween.TweenProperty(_kurtyna, "modulate:a", 1.0f, 1.0f); 
				await ToSignal(tween, "finished");
				
				_panelInstrukcji.Visible = false;
				await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
				
				var tweenOut = GetTree().CreateTween();
				tweenOut.TweenProperty(_kurtyna, "modulate:a", 0.0f, 1.5f);
				await ToSignal(tweenOut, "finished");
			}
			else
			{
				_panelInstrukcji.Visible = false;
			}
			
			_wejscie.Editable = true;
			_wejscie.GrabFocus();
		};
	}

	public override void _Process(double delta)
	{
		if (_wejscie.Editable && !_wejscie.HasFocus()) _wejscie.GrabFocus();
	}

	private void NaWpisanieTekstu(string tekst)
	{
		if (tekst.ToLower() == "r" || tekst.ToLower() == "reset") { ZresetujRysunek(); return; }
		if (tekst == "666") { PokazMenuWygranej(true); _wejscie.Clear(); return; }

		string czysty = tekst.Replace("@", "");
		string[] czesci = czysty.Split(',');
		if (czesci.Length == 2 && float.TryParse(czesci[0], out float x) && float.TryParse(czesci[1], out float y))
		{
			Vector2 p = new Vector2(x * _skalaX, -y * _skalaY);
			Vector2 nowy = tekst.StartsWith("@") ? _ostatniPunkt + p : p;
			
			_wszystkiePunkty.Add(_ostatniPunkt); 
			_wszystkiePunkty.Add(nowy);
			_ostatniPunkt = nowy; 
			
			_wejscie.Clear(); 
			DopasujKamere(); 
			QueueRedraw(); 
			
			SprawdzPunkt(nowy);
		}
	}

	private void ZresetujRysunek()
	{
		_wszystkiePunkty.Clear();
		_ostatniPunkt = Vector2.Zero;
		_ktoryPunktWzoru = 1;
		QueueRedraw();
		_wejscie.Clear();
	}

	private void AktualizujLicznikZyc()
	{
		if (_labelZycia != null)
		{
			_labelZycia.Text = $"Pozostało prób: {_liczbaZyc}/{_maxZyc}";
			
			if (_liczbaZyc <= 2) _labelZycia.Modulate = Colors.Red; 
			else _labelZycia.Modulate = Colors.White;
		}
	}

	private async void SprawdzPunkt(Vector2 punkt)
	{
		var wzor = _wszystkieZadania[_aktualneZadanieIndex];
		if (_ktoryPunktWzoru < wzor.Count)
		{
			Vector2 cel = new Vector2(wzor[_ktoryPunktWzoru].X * _skalaX, wzor[_ktoryPunktWzoru].Y * _skalaY);
			float dystans = punkt.DistanceTo(cel);

			// MARGINES BŁĘDU: 50 JEDNOSTEK
			if (dystans < 50.0f) 
			{ 
				// DOBRZE
				_ktoryPunktWzoru++; 
				if (_ktoryPunktWzoru >= wzor.Count) 
				{
					if (_audioWin != null) _audioWin.Play();
					PokazMenuWygranej(true); 
				}
			}
			else
			{
				// --- BŁĄD! (POWYŻEJ 50) ---
				
				_liczbaZyc--;
				
				// DŹWIĘK FAILA
				if (_audioFail != null) _audioFail.Play();
				
				// ODKRYWAMY LICZNIK PRÓB
				if (_labelZycia != null) 
				{
					_labelZycia.Visible = true;
					AktualizujLicznikZyc(); 
				}
				
				// CZERWONY BŁYSK
				if (_kurtyna != null)
				{
					_kurtyna.Color = new Color(1, 0, 0, 0.5f); 
					var tween = GetTree().CreateTween();
					tween.TweenProperty(_kurtyna, "modulate:a", 1.0f, 0.1f);
					tween.TweenProperty(_kurtyna, "modulate:a", 0.0f, 0.2f); 
					await ToSignal(tween, "finished");
					_kurtyna.Color = Colors.Black; 
				}

				if (_liczbaZyc <= 0) 
				{
					if (_audioWybuch != null) _audioWybuch.Play();
					PokazMenuWygranej(false); 
				}
			}
		}
	}

	private void PokazMenuWygranej(bool czyWygrana) 
	{ 
		_panelWygranej.Visible = true; 
		_wejscie.Editable = false; 
		
		if (_labelZycia != null) _labelZycia.Visible = false;

		if (czyWygrana)
		{
			if (_aktualneZadanieIndex == _wszystkieZadania.Count - 1) { _napisWygranej.Text = "GRATULACJE! UKOŃCZONO GRĘ!"; _btnDalej.Text = "MENU GŁÓWNE"; }
			else { _napisWygranej.Text = "ŚRUBA WYKONANA POPRAWNIE!"; _btnDalej.Text = "NASTĘPNE ZADANIE"; }
			
			_liczbaZyc = _maxZyc; 
		}
		else
		{
			_napisWygranej.Text = "AWARIA MASZYNY! (GAME OVER)";
			_btnDalej.Text = "WRÓĆ DO MENU";
			_aktualneZadanieIndex = -99; 
		}
	}

	private void PrzejdzDoNastepnejSruby()
	{
		if (_aktualneZadanieIndex == -99 || _aktualneZadanieIndex >= _wszystkieZadania.Count - 1) { GetTree().ChangeSceneToFile("res://Scenes/A_J/MenuGlowne.tscn"); return; }
		_aktualneZadanieIndex++; 
		_panelWygranej.Visible = false; 
		_wejscie.Editable = true; 
		
		if (_labelZycia != null) 
		{
			_labelZycia.Visible = false;
			_liczbaZyc = _maxZyc;
			AktualizujLicznikZyc();
		}

		_ktoryPunktWzoru = 1;

		if (_aktualneZadanieIndex < _wszystkieZadania.Count)
		{
			ZresetujRysunek();
			_kamera.Zoom = Vector2.One;
			_kamera.Position = _srodek;
			var podklad = GetNode<TextureRect>("PodkladSruby"); 
			string nazwaPliku = _nazwyObrazkow[_aktualneZadanieIndex];
			string sciezka = $"res://IMG/MenuA_J/{nazwaPliku}.png";
			if (FileAccess.FileExists(sciezka)) podklad.Texture = GD.Load<Texture2D>(sciezka);
			QueueRedraw(); 
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb) 
		{
			if (mb.ButtonIndex == MouseButton.WheelUp) { if (_kamera.Zoom.X < 4.0f) _kamera.Zoom += new Vector2(_czuloscZoomu, _czuloscZoomu); }
			else if (mb.ButtonIndex == MouseButton.WheelDown) { if (_kamera.Zoom.X > 0.5f) _kamera.Zoom -= new Vector2(_czuloscZoomu, _czuloscZoomu); }
			if (mb.ButtonIndex == MouseButton.Right) _czyPrzesuwa = mb.Pressed;
		}
		if (@event is InputEventMouseMotion mm && _czyPrzesuwa) _kamera.Position -= mm.Relative / _kamera.Zoom.X;
	}

	private void DopasujKamere()
	{
		if (_wszystkiePunkty.Count == 0) return;
		Vector2 centrum = new Vector2((_wszystkiePunkty.Min(p => p.X) + _wszystkiePunkty.Max(p => p.X)) / 2, (_wszystkiePunkty.Min(p => p.Y) + _wszystkiePunkty.Max(p => p.Y)) / 2);
		GetTree().CreateTween().TweenProperty(_kamera, "position", centrum + _srodek, 0.5f);
	}

	public override void _Draw()
	{
		DrawLine(new Vector2(-5000, 0) + _srodek, new Vector2(5000, 0) + _srodek, Colors.DimGray, 1 / _kamera.Zoom.X);
		DrawLine(new Vector2(0, -5000) + _srodek, new Vector2(0, 5000) + _srodek, Colors.DimGray, 1 / _kamera.Zoom.X);
		for (int i = 0; i < _wszystkiePunkty.Count; i += 2) 
		{
			DrawLine(_wszystkiePunkty[i] + _srodek, _wszystkiePunkty[i+1] + _srodek, Colors.OrangeRed, 5.0f / _kamera.Zoom.X, true);
		}
		DrawCircle(_ostatniPunkt + _srodek, 6.0f / _kamera.Zoom.X, Colors.Yellow);
	}
}
