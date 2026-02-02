using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public partial class LogikaGry : Node2D
{
	// ==========================================
	// ZMIENNE
	// ==========================================
	private List<Vector2> _wszystkiePunkty = new List<Vector2>();
	private Vector2 _ostatniPunkt = Vector2.Zero;
	private LineEdit _wejscie;
	private Camera2D _kamera;
	private Vector2 _srodek;
	
	// UI
	private CanvasLayer _panelWygranej;
	private CanvasLayer _panelInstrukcji;
	private CanvasLayer _panelFabuly;
	
	// WARSTWA SPECJALNA (Tylko piesek, wybuch robimy kodem)
	private CanvasLayer _warstwaPieska; 
	
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
	private AudioStreamPlayer _audioPiesek; 
	
	private AudioStreamPlayer _audioFinalBoss; 
	private AudioStreamPlayer _audioKeyboard; 
	
	// ZMIENNE ROZGRYWKI
	private bool _czyPrzesuwa = false;
	private float _czuloscZoomu = 0.1f;
	private int _liczbaZyc = 5; 
	private int _maxZyc = 5;

	private List<List<Vector2>> _wszystkieZadania = new List<List<Vector2>>();
	private List<string> _nazwyObrazkow = new List<string> { "T_A_J", "Sruba_2", "Sruba_3" }; 
	
	private List<Vector2> _skalePoziomow = new List<Vector2>();
	private float _aktualnaSkalaX = 1.0f; 
	private float _aktualnaSkalaY = 1.0f; 

	private int _aktualneZadanieIndex = 0;
	private int _ktoryPunktWzoru = 1; 

	// Zmienne do skipowania tekstu spacją
	private bool _czyPisanieTrwa = false;
	private bool _skipujPisanie = false;


	// ==========================================
	// FUNKCJA _READY (START GRY)
	// ==========================================
	public override void _Ready()
	{
		_srodek = GetViewportRect().Size / 2;
		
		_skalePoziomow.Add(new Vector2(2.75f, 2.7f)); 
		_skalePoziomow.Add(new Vector2(4.0f, 4.0f));  
		_skalePoziomow.Add(new Vector2(3.2f, 3.2f));  

		if (_skalePoziomow.Count > 0)
		{
			_aktualnaSkalaX = _skalePoziomow[0].X;
			_aktualnaSkalaY = _skalePoziomow[0].Y;
		}

		_wejscie = GetNode<LineEdit>("CanvasLayer/LineEdit");
		_kamera = GetNode<Camera2D>("Camera2D");
		
		_panelWygranej = GetNode<CanvasLayer>("WarstwaWygranej");
		_panelInstrukcji = GetNode<CanvasLayer>("CanvasLayer2");
		
		_btnDalej = GetNode<Button>("WarstwaWygranej/Panel/PrzyciskDalej");
		_napisWygranej = GetNode<Label>("WarstwaWygranej/Panel/Label");
		_btnStart = GetNode<Button>("CanvasLayer2/Panel/Button");

		_warstwaPieska = GetNodeOrNull<CanvasLayer>("WarstwaPieska");
		if (_warstwaPieska != null) _warstwaPieska.Visible = false;

		_labelZycia = GetNodeOrNull<Label>("CanvasLayer5/LabelZycia");
		
		if (_labelZycia != null) 
		{
			_labelZycia.Visible = false; 
			_labelZycia.Text = $"Pozostało prób: {_liczbaZyc}/{_maxZyc}";
		}

		_audioMenuMusic = GetNodeOrNull<AudioStreamPlayer>("Audio_MenuMusic");
		_audioSilnik = GetNodeOrNull<AudioStreamPlayer>("Audio_Silnik");
		_audioBieg = GetNodeOrNull<AudioStreamPlayer>("Audio_Bieg");
		_audioKlik = GetNodeOrNull<AudioStreamPlayer>("Audio_Klik");
		_audioWin = GetNodeOrNull<AudioStreamPlayer>("Audio_Win");
		_audioWybuch = GetNodeOrNull<AudioStreamPlayer>("Audio_Wybuch");
		_audioFail = GetNodeOrNull<AudioStreamPlayer>("Audio_Fail");
		_audioFinalBoss = GetNodeOrNull<AudioStreamPlayer>("Audio_FinalBoss");
		_audioKeyboard = GetNodeOrNull<AudioStreamPlayer>("Audio_Keyboard");
		_audioPiesek = GetNodeOrNull<AudioStreamPlayer>("Audio_Piesek");

		_kurtyna = GetNodeOrNull<ColorRect>("CanvasLayer4/Kurtyna"); 
		if (_kurtyna != null)
		{
			_kurtyna.Visible = true;
			_kurtyna.Modulate = new Color(0, 0, 0, 0); 
			_kurtyna.Color = Colors.Black;
			_kurtyna.MouseFilter = Control.MouseFilterEnum.Ignore; 
		}

		_panelFabuly = GetNodeOrNull<CanvasLayer>("WarstwaFabuly");
		if (_panelFabuly != null)
		{
			_btnMisja = _panelFabuly.GetNodeOrNull<Button>("Panel/PrzyciskMisji");
			if (_btnMisja == null) _btnMisja = _panelFabuly.GetNodeOrNull<Button>("PrzyciskMisji");
			
			_napisFabuly = _panelFabuly.GetNodeOrNull<Label>("Panel/Label");
			if (_napisFabuly == null) _napisFabuly = _panelFabuly.GetNodeOrNull<Label>("Label");
			
			if (_napisFabuly != null)
			{
				string historia = "Uciekła gdzieś śrubka...\nGdzie jest wspornik!?\nCo my zrobimy?!\n\nHej Ty! Wyglądasz na kogoś,\nkto zna się na robocie.\nBierz się do pracy!";
				
				UstawTloPlazmaV2(_panelFabuly, new Color(0.1f, 0.0f, 0.3f), new Color(0.0f, 0.6f, 1.0f));
				PrzygotujTloPodTekstem(_napisFabuly);
				
				WypiszTekstZeDzwiekiem(_napisFabuly, historia, _btnMisja);
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
		
		UstawTloPlazmaV2(_panelInstrukcji, new Color(0.15f, 0.0f, 0.15f), new Color(0.9f, 0.0f, 0.5f));
		DodajTloPlazmaDoWarstwy(_panelWygranej, new Color(0.05f, 0.05f, 0.05f), new Color(0.2f, 0.2f, 0.2f));

		_wszystkieZadania.Add(new List<Vector2> { new Vector2(0, 0), new Vector2(20, 0), new Vector2(20, -80), new Vector2(50, -80), new Vector2(50, -100), new Vector2(-30, -100), new Vector2(-30, -80), new Vector2(0, -80), new Vector2(0, 0) });
		_wszystkieZadania.Add(new List<Vector2> { new Vector2(0, 0), new Vector2(40, 0), new Vector2(0, -30), new Vector2(0, 0) });
		_wszystkieZadania.Add(new List<Vector2> { new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, -40), new Vector2(47, -40), new Vector2(47, -60), new Vector2(31, -60), new Vector2(31, -70), new Vector2(8, -70), new Vector2(8, -42), new Vector2(0, -42), new Vector2(0, 0) });

		_wejscie.TextSubmitted += NaWpisanieTekstu;
		
		_btnDalej.Pressed += async () => {
			if (_audioKlik != null) _audioKlik.Play();
			await ToSignal(GetTree().CreateTimer(0.15f), "timeout");
			PrzejdzDoNastepnejSruby();
		};

		if (_btnMisja != null)
		{
			_btnMisja.Pressed += async () => {
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

		_btnStart.Pressed += async () => {
			if (_audioKlik != null) _audioKlik.Play();
			if (_audioMenuMusic != null) _audioMenuMusic.Stop();

			CanvasLayer tempLayer = new CanvasLayer();
			tempLayer.Layer = 1000; 
			GetTree().Root.AddChild(tempLayer);

			ColorRect tempRect = new ColorRect();
			tempRect.Color = Colors.Black;
			tempRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			tempRect.MouseFilter = Control.MouseFilterEnum.Ignore;
			tempRect.Modulate = new Color(0, 0, 0, 0); 
			tempLayer.AddChild(tempRect);

			var tweenIn = tempLayer.CreateTween();
			tweenIn.TweenProperty(tempRect, "modulate:a", 1.0f, 0.25f);
			await ToSignal(tweenIn, "finished");

			_panelInstrukcji.Visible = false;
			await ToSignal(GetTree().CreateTimer(0.15f), "timeout");

			var tweenOut = tempLayer.CreateTween();
			tweenOut.TweenProperty(tempRect, "modulate:a", 0.0f, 0.45f);
			await ToSignal(tweenOut, "finished");

			tempLayer.QueueFree(); 

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
		
		if (tekst == "666") 
		{ 
			_wejscie.Clear(); 
			if (_aktualneZadanieIndex >= _wszystkieZadania.Count - 1) 
			{
				FinalneSciemnienie(); 
			}
			else 
			{
				PokazMenuWygranej(true); 
			}
			return; 
		}

		string czysty = tekst.Replace("@", "");
		string[] czesci = czysty.Split(',');
		if (czesci.Length == 2 && float.TryParse(czesci[0], out float x) && float.TryParse(czesci[1], out float y))
		{
			Vector2 p = new Vector2(x * _aktualnaSkalaX, -y * _aktualnaSkalaY);
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
		if (_ktoryPunktWzoru >= wzor.Count) return;

		Vector2 celBaza = wzor[_ktoryPunktWzoru]; 
		Vector2 celEkran = new Vector2(celBaza.X * _aktualnaSkalaX, celBaza.Y * _aktualnaSkalaY);
		float dystans = punkt.DistanceTo(celEkran);

		if (dystans < 50.0f) 
		{ 
			_ktoryPunktWzoru++; 
			if (_ktoryPunktWzoru >= wzor.Count) 
			{
				if (_aktualneZadanieIndex >= _wszystkieZadania.Count - 1)
				{
					FinalneSciemnienie(); 
				}
				else
				{
					PokazMenuWygranej(true); 
				}
			}
		}
		else
		{
			_liczbaZyc--;
			if (_audioFail != null) _audioFail.Play();
			if (_labelZycia != null) { _labelZycia.Visible = true; AktualizujLicznikZyc(); }
			
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
				await PokazPieskaSmierci();
				await OdpalamyWielkiWybuch(); 
				PokazMenuWygranej(false); 
			}
		}
	}

	private async Task PokazPieskaSmierci()
	{
		if (_warstwaPieska == null) return;

		_warstwaPieska.Visible = true;
		if (_audioPiesek != null) _audioPiesek.Play();

		await ToSignal(GetTree().CreateTimer(3.0f), "timeout");

		if (_audioPiesek != null) _audioPiesek.Stop();
		_warstwaPieska.Visible = false;
	}

	private async Task OdpalamyWielkiWybuch()
	{
		CanvasLayer warstwaEksplozji = new CanvasLayer();
		warstwaEksplozji.Layer = 2000; 
		GetTree().Root.AddChild(warstwaEksplozji);

		ColorRect flash = new ColorRect();
		flash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		flash.Color = new Color(1, 0, 0, 0.3f);
		warstwaEksplozji.AddChild(flash);

		CpuParticles2D wybuch = new CpuParticles2D();

		GradientTexture2D tex = new GradientTexture2D();
		tex.Width = 4; tex.Height = 4;
		tex.Fill = GradientTexture2D.FillEnum.Square;
		Gradient g = new Gradient();
		g.SetColor(0, Colors.White);
		g.SetColor(1, Colors.White);
		tex.Gradient = g;
		wybuch.Texture = tex;

		wybuch.OneShot = true;
		wybuch.Explosiveness = 1.0f;
		wybuch.Amount = 500; 
		wybuch.Lifetime = 2.5f;
		wybuch.Spread = 180; 
		wybuch.Gravity = Vector2.Zero;
		wybuch.InitialVelocityMin = 300;
		wybuch.InitialVelocityMax = 1000;
		wybuch.ScaleAmountMin = 5;
		wybuch.ScaleAmountMax = 25;

		Gradient gradPart = new Gradient();
		gradPart.SetColor(0, Colors.Red);
		gradPart.SetColor(1, Colors.Orange);
		gradPart.SetColor(2, Colors.Yellow);
		gradPart.SetColor(3, new Color(0, 0, 0, 0));
		wybuch.ColorRamp = gradPart;

		wybuch.Position = _srodek;

		warstwaEksplozji.AddChild(wybuch);

		if (_audioWybuch != null) _audioWybuch.Play();
		wybuch.Emitting = true;

		await ToSignal(GetTree().CreateTimer(3.0f), "timeout");
		warstwaEksplozji.QueueFree();
	}

	private void PokazMenuWygranej(bool czyWygrana) 
	{ 
		_panelWygranej.Visible = true; 
		_wejscie.Editable = false; 
		if (_labelZycia != null) _labelZycia.Visible = false;

		_napisWygranej.Modulate = Colors.White;

		var panelWewnatrz = _panelWygranej.GetNodeOrNull<Panel>("Panel");
		
		if (panelWewnatrz != null)
		{
			Vector2 rozmiarRamki = new Vector2(600, 200);
			panelWewnatrz.Size = rozmiarRamki;
			panelWewnatrz.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
			panelWewnatrz.PivotOffset = rozmiarRamki / 2;
			panelWewnatrz.Position = _srodek - (rozmiarRamki / 2);

			_napisWygranej.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			_napisWygranej.HorizontalAlignment = HorizontalAlignment.Center;
			_napisWygranej.VerticalAlignment = VerticalAlignment.Center;
			_napisWygranej.AddThemeConstantOverride("margin_left", 0);
			_napisWygranej.AddThemeConstantOverride("margin_top", 0);
			_napisWygranej.AddThemeConstantOverride("margin_right", 0);
			_napisWygranej.AddThemeConstantOverride("margin_bottom", 0);
			
			_napisWygranej.AddThemeFontSizeOverride("font_size", 45); 
			_btnDalej.AddThemeFontSizeOverride("font_size", 30);

			_btnDalej.CustomMinimumSize = new Vector2(350, 60);
			_btnDalej.Size = Vector2.Zero; 

			float btnX = (rozmiarRamki.X - _btnDalej.CustomMinimumSize.X) / 2;
			_btnDalej.Position = new Vector2(btnX, rozmiarRamki.Y + 40);
			
			if (czyWygrana)
			{
				UstawStylKartyWyniku(panelWewnatrz, new Color(0, 1, 0), new Color(0, 0.2f, 0, 0.9f));
			}
			else
			{
				UstawStylKartyWyniku(panelWewnatrz, new Color(1, 0, 0), new Color(0.2f, 0, 0, 0.9f));
			}

			panelWewnatrz.Scale = Vector2.Zero;
			GetTree().CreateTween().TweenProperty(panelWewnatrz, "scale", Vector2.One, 0.4f)
				.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		}

		if (czyWygrana)
		{
			if (_audioWin != null) _audioWin.Play();

			Color neonFlash = new Color(0.5f, 1.5f, 0.5f); 
			Color neonSteady = new Color(0.2f, 1.0f, 0.2f); 

			var tweenGlow = GetTree().CreateTween();
			tweenGlow.TweenProperty(_napisWygranej, "modulate", neonFlash, 0.2f)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
			tweenGlow.TweenProperty(_napisWygranej, "modulate", neonSteady, 0.3f)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);

			// --- TUTAJ ZMIENIONE NAPISY DLA KAŻDEJ ŚRUBY OSOBNO ---
			if (_aktualneZadanieIndex == _wszystkieZadania.Count - 1) 
			{ 
				_napisWygranej.Text = "GRATULACJE!\nUKOŃCZONO GRĘ!"; 
				_btnDalej.Text = "MENU GŁÓWNE"; 
			}
			else 
			{ 
				switch(_aktualneZadanieIndex)
				{
					case 0:
						_napisWygranej.Text = "SYSTEM: ŚRUBA POPRAWNA!";
						break;
					case 1:
						_napisWygranej.Text = "SYSTEM: WSPORNIK POPRAWNY!";
						break;
					default:
						_napisWygranej.Text = "SYSTEM: ELEMENT POPRAWNY!";
						break;
				}
				_btnDalej.Text = "NASTĘPNE ZADANIE"; 
			}
			_liczbaZyc = _maxZyc; 
		}
		else
		{
			_napisWygranej.Text = "BŁĄD KRYTYCZNY!";
			_btnDalej.Text = "RESTART SYSTEMU";
			_aktualneZadanieIndex = -99; 

			var tweenBłąd = GetTree().CreateTween().SetLoops(); 
			tweenBłąd.TweenProperty(_napisWygranej, "modulate", new Color(1.5f, 0.0f, 0.0f), 0.2f);
			tweenBłąd.TweenProperty(_napisWygranej, "modulate", new Color(1, 1, 1), 0.2f);
		}
	}

	private async void FinalneSciemnienie()
	{
		_wejscie.Editable = false;
		if (_audioWin != null) _audioWin.Play();

		if (_kurtyna != null)
		{
			_kurtyna.Visible = true;
			_kurtyna.Modulate = new Color(0, 0, 0, 0);
			_kurtyna.Color = Colors.Black;

			var tween = GetTree().CreateTween();
			tween.TweenProperty(_kurtyna, "modulate:a", 1.0f, 1.5f); 
			await ToSignal(tween, "finished");
		}

		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
		GetTree().ChangeSceneToFile("res://Scenes/A_J/EkranKoncowy.tscn");
	}

	private void PrzejdzDoNastepnejSruby()
	{
		if (_aktualneZadanieIndex == -99 || _aktualneZadanieIndex >= _wszystkieZadania.Count - 1) 
		{ 
			GetTree().ChangeSceneToFile("res://Scenes/A_J/MenuGlowne.tscn"); 
			return; 
		}

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
			if (_aktualneZadanieIndex < _skalePoziomow.Count)
			{
				_aktualnaSkalaX = _skalePoziomow[_aktualneZadanieIndex].X;
				_aktualnaSkalaY = _skalePoziomow[_aktualneZadanieIndex].Y;
			}
			
			if (_aktualneZadanieIndex == _wszystkieZadania.Count - 1) 
			{
				if (_audioSilnik != null) _audioSilnik.Stop(); 
				if (_audioFinalBoss != null) _audioFinalBoss.Play(); 
			}
			
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

		if (@event is InputEventKey k && k.Pressed && k.Keycode == Key.Space)
		{
			if (_czyPisanieTrwa) _skipujPisanie = true;
		}
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
		for (int i = 0; i < _wszystkiePunkty.Count; i += 2) DrawLine(_wszystkiePunkty[i] + _srodek, _wszystkiePunkty[i+1] + _srodek, Colors.OrangeRed, 5.0f / _kamera.Zoom.X, true);
		DrawCircle(_ostatniPunkt + _srodek, 6.0f / _kamera.Zoom.X, Colors.Yellow);
	}

	private async void WypiszTekstZeDzwiekiem(Label label, string calyTekst, Button przyciskDoPokazania)
	{
		if (przyciskDoPokazania != null) przyciskDoPokazania.Visible = false;
		
		Panel tloTekstu = label.GetNodeOrNull<Panel>("TloTekstu");
		if (tloTekstu != null)
		{
			tloTekstu.Modulate = new Color(1, 1, 1, 0); 
			tloTekstu.Scale = new Vector2(0.9f, 0.9f); 
			tloTekstu.PivotOffset = tloTekstu.Size / 2; 

			var tween = GetTree().CreateTween().SetParallel(true);
			tween.TweenProperty(tloTekstu, "modulate:a", 1.0f, 0.5f);
			tween.TweenProperty(tloTekstu, "scale", Vector2.One, 0.5f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		}

		label.Text = calyTekst; 
		label.VisibleCharacters = 0; 
		float szybkoscPisania = 0.04f; 
		
		_czyPisanieTrwa = true;
		_skipujPisanie = false; 
		
		for (int i = 0; i < calyTekst.Length; i++)
		{
			if (_skipujPisanie)
			{
				label.VisibleCharacters = -1;
				break; 
			}

			label.VisibleCharacters = i + 1; 
			if (i % 5 == 0 && !char.IsWhiteSpace(calyTekst[i])) 
			{
				if (_audioKeyboard != null)
				{
					_audioKeyboard.PitchScale = (float)GD.RandRange(0.95, 1.05);
					_audioKeyboard.Play();
				}
			}
			await ToSignal(GetTree().CreateTimer(szybkoscPisania), "timeout");
		}
		
		_czyPisanieTrwa = false;
		label.VisibleCharacters = -1; 
		
		if (_audioKeyboard != null) _audioKeyboard.Stop(); 
		
		if (przyciskDoPokazania != null) przyciskDoPokazania.Visible = true;
	}

	private void UstawTloPlazmaV2(CanvasLayer warstwa, Color kolorCiemny, Color kolorJasny)
	{
		if (warstwa == null) return;
		var panel = warstwa.GetNodeOrNull<Panel>("Panel");
		if (panel == null) return;

		foreach (var child in panel.GetChildren())
		{
			if (child is ColorRect t && t.Name == "TloPlazma") child.QueueFree();
		}

		ColorRect tlo = new ColorRect();
		tlo.Name = "TloPlazma";
		tlo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		tlo.MouseFilter = Control.MouseFilterEnum.Ignore;
		
		ShaderMaterial mat = new ShaderMaterial();
		Shader shader = new Shader();
		shader.Code = @"
			shader_type canvas_item;
			uniform vec4 deep_color : source_color;
			uniform vec4 light_color : source_color;

			void fragment() {
				vec2 uv = UV;
				float t = TIME * 0.2;
				vec2 p = uv * 3.0;
				float q = sin(p.x + sin(t * 0.5) + t);
				float r = sin(p.y - cos(t * 0.3) + t * 0.7);
				float wave = sin(q * 2.0 + r * 2.0 + t);
				wave = wave * 0.5 + 0.5; 
				float detail = sin(uv.x * 10.0 + uv.y * 5.0 + t * 2.0) * 0.1;
				float final_mask = wave + detail;
				vec4 color = mix(deep_color, light_color, final_mask);
				float dist = distance(uv, vec2(0.5));
				color.rgb *= smoothstep(0.9, 0.2, dist);
				COLOR = color;
			}
		";
		mat.Shader = shader;
		mat.SetShaderParameter("deep_color", kolorCiemny);
		mat.SetShaderParameter("light_color", kolorJasny);
		
		tlo.Material = mat;
		panel.AddChild(tlo);
		panel.MoveChild(tlo, 0);

		var style = new StyleBoxFlat();
		style.BgColor = new Color(0,0,0,0);
		style.BorderColor = new Color(1,1,1,0.3f);
		style.BorderWidthBottom = 1; style.BorderWidthTop = 1; style.BorderWidthLeft = 1; style.BorderWidthRight = 1;
		style.CornerRadiusTopLeft = 15; style.CornerRadiusTopRight = 15; style.CornerRadiusBottomRight = 15; style.CornerRadiusBottomLeft = 15;
		panel.AddThemeStyleboxOverride("panel", style);
	}

	private void PrzygotujTloPodTekstem(Label label)
	{
		if (label == null) return;
		var stare = label.GetNodeOrNull<Panel>("TloTekstu");
		if (stare != null) stare.QueueFree();

		Panel tlo = new Panel();
		tlo.Name = "TloTekstu";
		tlo.ShowBehindParent = true;
		tlo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		
		var style = new StyleBoxFlat();
		style.BgColor = new Color(0.08f, 0.08f, 0.1f, 0.9f); 
		style.BorderColor = new Color(1.0f, 1.0f, 1.0f, 0.2f); 
		style.BorderWidthTop = 1; style.BorderWidthBottom = 1; style.BorderWidthLeft = 1; style.BorderWidthRight = 1;
		style.CornerRadiusTopLeft = 8; style.CornerRadiusTopRight = 8; style.CornerRadiusBottomRight = 8; style.CornerRadiusBottomLeft = 8;
		style.ShadowColor = new Color(0, 0, 0, 0.8f);
		style.ShadowSize = 10;
		style.ExpandMarginLeft = 25; style.ExpandMarginRight = 25; style.ExpandMarginTop = 15; style.ExpandMarginBottom = 15;
		
		tlo.AddThemeStyleboxOverride("panel", style);
		label.AddChild(tlo);

		Color kolorZolty = new Color(1f, 0.9f, 0.2f);
		int rozmiarPiksela = 6;

		PikselowyWykrzyknik wykrzyknikPrawy = new PikselowyWykrzyknik(kolorZolty, rozmiarPiksela);
		wykrzyknikPrawy.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
		wykrzyknikPrawy.Position = new Vector2(-50, 20); 
		tlo.AddChild(wykrzyknikPrawy);

		PikselowyWykrzyknik wykrzyknikLewy = new PikselowyWykrzyknik(kolorZolty, rozmiarPiksela);
		wykrzyknikLewy.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
		wykrzyknikLewy.Position = new Vector2(50, 20); 
		tlo.AddChild(wykrzyknikLewy);

		Vector2 pivot = new Vector2(3 * rozmiarPiksela / 2f, 8 * rozmiarPiksela / 2f); 
		wykrzyknikPrawy.PivotOffset = pivot;
		wykrzyknikLewy.PivotOffset = pivot;

		var tween = GetTree().CreateTween().SetLoops().SetParallel(true);
		tween.TweenProperty(wykrzyknikPrawy, "scale", new Vector2(1.15f, 1.15f), 0.7f).SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(wykrzyknikLewy, "scale", new Vector2(1.15f, 1.15f), 0.7f).SetTrans(Tween.TransitionType.Sine);
		
		tween.Chain().TweenProperty(wykrzyknikPrawy, "scale", Vector2.One, 0.7f).SetTrans(Tween.TransitionType.Sine);
		tween.Parallel().TweenProperty(wykrzyknikLewy, "scale", Vector2.One, 0.7f).SetTrans(Tween.TransitionType.Sine);
	}

	private void UstawStylKartyWyniku(Panel panel, Color kolorAkcentu, Color kolorTla)
	{
		var style = new StyleBoxFlat();
		style.BgColor = kolorTla;
		style.BorderColor = kolorAkcentu;
		style.BorderWidthTop = 2; style.BorderWidthBottom = 2; style.BorderWidthLeft = 2; style.BorderWidthRight = 2;
		style.CornerRadiusTopLeft = 0; style.CornerRadiusTopRight = 0; style.CornerRadiusBottomRight = 0; style.CornerRadiusBottomLeft = 0;
		style.ShadowColor = new Color(kolorAkcentu.R, kolorAkcentu.G, kolorAkcentu.B, 0.6f);
		style.ShadowSize = 30; 
		style.ShadowOffset = Vector2.Zero; 
		panel.AddThemeStyleboxOverride("panel", style);
	}
	
	private void DodajTloPlazmaDoWarstwy(CanvasLayer warstwa, Color kolorCiemny, Color kolorJasny)
	{
		if (warstwa == null) return;
		foreach(var c in warstwa.GetChildren()) { if (c.Name == "TloPlazmaGlobal") c.QueueFree(); }

		ColorRect tlo = new ColorRect();
		tlo.Name = "TloPlazmaGlobal";
		tlo.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		tlo.MouseFilter = Control.MouseFilterEnum.Ignore;
		
		ShaderMaterial mat = new ShaderMaterial();
		Shader shader = new Shader();
		shader.Code = @"
			shader_type canvas_item;
			uniform vec4 deep_color : source_color;
			uniform vec4 light_color : source_color;

			void fragment() {
				vec2 uv = UV;
				float t = TIME * 0.2;
				vec2 p = uv * 3.0;
				float q = sin(p.x + sin(t * 0.5) + t);
				float r = sin(p.y - cos(t * 0.3) + t * 0.7);
				float wave = sin(q * 2.0 + r * 2.0 + t);
				wave = wave * 0.5 + 0.5; 
				float detail = sin(uv.x * 10.0 + uv.y * 5.0 + t * 2.0) * 0.1;
				float final_mask = wave + detail;
				vec4 color = mix(deep_color, light_color, final_mask);
				float dist = distance(uv, vec2(0.5));
				color.rgb *= smoothstep(0.9, 0.2, dist);
				COLOR = color;
			}
		";
		mat.Shader = shader;
		mat.SetShaderParameter("deep_color", kolorCiemny);
		mat.SetShaderParameter("light_color", kolorJasny);
		tlo.Material = mat;

		warstwa.AddChild(tlo);
		warstwa.MoveChild(tlo, 0); 
	}
}

public partial class PikselowyWykrzyknik : Control
{
	private Color _kolor;
	private int _rozmiarPiksela;

	public PikselowyWykrzyknik(Color kolor, int rozmiarPiksela)
	{
		_kolor = kolor;
		_rozmiarPiksela = rozmiarPiksela;
		this.Modulate = Colors.White; 
	}

	public override void _Draw()
	{
		int[,] ksztalt = new int[,] {
			{0, 1, 0},
			{0, 1, 0},
			{0, 1, 0},
			{0, 1, 0},
			{0, 1, 0},
			{0, 0, 0},
			{0, 1, 0},
			{0, 1, 0}
		};

		for (int y = 0; y < ksztalt.GetLength(0); y++)
		{
			for (int x = 0; x < ksztalt.GetLength(1); x++)
			{
				if (ksztalt[y, x] == 1)
				{
					DrawRect(new Rect2(x * _rozmiarPiksela, y * _rozmiarPiksela, _rozmiarPiksela, _rozmiarPiksela), _kolor);
				}
			}
		}
	}
}
