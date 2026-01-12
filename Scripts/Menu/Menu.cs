using Godot;
using System;
using System.Threading.Tasks;

// Klasa Menu dopasowana do pliku Menu.cs - Styl Lo-Fi Cyberpunk (Spokojny i Retro)
public partial class Menu : Control
{
	// --- KONFIGURACJA KOLORÓW LO-FI ---
	private readonly Color AMBER = new Color(1.0f, 0.65f, 0.0f, 0.9f); // Klasyczny bursztyn CRT
	private readonly Color AMBER_DIM = new Color(1.0f, 0.65f, 0.0f, 0.4f);
	private readonly Color BG_DARK = new Color(0.05f, 0.04f, 0.03f, 1.0f); // Bardzo ciemny brąz/czerń

	// --- EDYTOWALNE PARAMETRY (Widoczne w Inspektorze) ---
	[ExportGroup("Ustawienia Systemowe")]
	[Export] public Vector2I TargetResolution = new Vector2I(1280, 720);
	[Export] public bool ForceResolutionOnStart = true;
	[Export] public string GameScenePath = "res://Scenes/Game.tscn"; // Ścieżka do Twojej gry

	[ExportGroup("Teksty Intro")]
	[Export] public string SplashTitle = "SKNI_CORE_v4.0";
	[Export] public string SplashSubtitle = "[ ANALOG FEED ACTIVE ]";
	
	[ExportGroup("Teksty Menu")]
	[Export] public string GameTitle = "PIXELFORGE";
	[Export] public string GameSubtitle = "INDUSTRIAL_SECTOR_07 // STABLE_ENVIRONMENT";

	[ExportGroup("Konfiguracja Przycisków")]
	[Export] public string[] ButtonLabels = { "INICJUJ PROCES", "TWÓRCY SYSTEMU", "PORTAL SKNI", "STRONA KOŁA", "TERMINACJA" };
	
	[ExportGroup("Stałe Informacje (Panel Prawy)")]
	[Export(PropertyHint.MultilineText)] public string StaticInfoText = "LOKALIZACJA: SEKTOR_7G\nSTATUS: MONITOROWANIE_ASYNC\nZASILANIE: 100% (NOMINALNE)\nPROCESY: OPTYMALNE\n--------------------------\nKERNEL_HASH: 0x88A21B\nMODUŁ_IO: SPRAWNY\nSYSTEM_UPTIME: 144h 12m\nPOŁĄCZENIE: SZYFROWANE_AES256";
	
	[ExportGroup("Twórcy (Pop-up)")]
	[Export(PropertyHint.MultilineText)] public string CreditsText = "STUDENCKIE KOŁO NAUKOWE\nINFORMATYKÓW UP\n\n>> PROTOKÓŁ KODOWANIA: ZESPÓŁ SKNI\n>> JEDNOSTKA GRAFICZNA: NEON_PIXEL\n>> KERNEL: GODOT 4 C# (RETRO_BUILD)";
	
	[ExportGroup("Sekwencja Ładowania Gry")]
	[Export] public string[] LoadingLines = {
		">> NAWIĄZYWANIE POŁĄCZENIA Z SEKTOREM...",
		">> OMINIĘCIE PROTOKOŁÓW BEZPIECZEŃSTWA...",
		">> DESZYFRACJA STRUMIENIA DANYCH...",
		">> SYNCHRONIZACJA SILNIKA FIZYCZNEGO...",
		">> TRANSFER_DANYCH_99%...",
        ">> TRANSMISJA ROZPOCZĘTA."
	};

	[ExportGroup("Linki Zewnętrzne")]
	[Export] public string WebsiteURL = "https://www.facebook.com/skni.up/";
	[Export] public string WebsiteURL2 = "https://skni.up.krakow.pl/"; 

	[ExportGroup("Sekwencja Bootowania")]
	[Export] public string[] BootLines = {
		">> INICJACJA MODUŁU KRYTYCZNEGO...",
		">> ODŚWIEŻANIE PAMIĘCI KACHOWEJ...",
		">> SPRAWDZANIE UPRAWNIEŃ SKNI...",
		">> DOSTĘP PRZYZNANY: POZIOM_ADMIN",
		">> URUCHAMIANIE SILNIKA PIXELFORGE...",
		">> SYNCHRONIZACJA Z SEKTOREM 07...",
        ">> SYSTEM GOTOWY DO PRACY."
	};

	[ExportGroup("Audio")]
	[Export] public AudioStream MusicStream;
	[Export] public AudioStream BootSoundStream;
	[Export] public AudioStream MenuLoadSoundStream;
	[Export] public AudioStream HoverSoundStream;
	[Export] public AudioStream ClickSoundStream;
	[Export] public AudioStream CrtOffSoundStream; // Dźwięk wyłączania monitora

	// --- Referencje do węzłów ---
	private CanvasLayer _layer;
	private Control _splashScreen;
	private Control _bootingScreen;
	private Control _mainMenu;
	private Control _loadingScreen;
	private RichTextLabel _bootText;
	private RichTextLabel _loadingText;
	private ColorRect _shaderRect;
	private VBoxContainer _buttonContainer;
	private Control _infoPanel;
	private Control _creditsPopup;
	private ColorRect _crtOffFlash; // Błysk CRT (teraz bursztynowy)

	private AudioStreamPlayer _musicPlayer;
	private AudioStreamPlayer _sfxPlayer;
	private AudioStreamPlayer _bootSfxPlayer;

	private ShaderMaterial _shaderMaterial;
	private const string GLITCH_PARAM = "glitch_intensity";
	private float _baseGlitch = 0.0008f; 
	private bool _isLoading = false;

	public override void _Ready()
	{
		// 1. Solidne wymuszenie rozdzielczości i skalowania UI
		if (ForceResolutionOnStart)
		{
			var root = GetTree().Root;
			root.ContentScaleSize = TargetResolution;
			root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
			root.ContentScaleAspect = Window.ContentScaleAspectEnum.Keep;

			var window = GetWindow();
			if (!DisplayServer.WindowGetMode().HasFlag(DisplayServer.WindowMode.Maximized))
			{
				try 
				{
					window.Size = TargetResolution;
					if (OS.HasFeature("standalone"))
					{
						var screenId = window.CurrentScreen;
						var screenRect = DisplayServer.ScreenGetPosition(screenId);
						var screenSize = DisplayServer.ScreenGetSize(screenId);
						window.Position = screenRect + (screenSize - TargetResolution) / 2;
					}
				}
				catch (Exception) {}
			}
		}

		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		EnsureSceneStructure();
		SetupAudioPlayers();

		if (_shaderRect?.Material is ShaderMaterial mat)
		{
			_shaderMaterial = mat;
			_shaderMaterial.SetShaderParameter(GLITCH_PARAM, _baseGlitch);
			_shaderMaterial.SetShaderParameter("distortion", 0.008f); 
		}

		BuildCyberpunkUI();
		
		_splashScreen.Visible = true;
		_bootingScreen.Visible = false;
		_mainMenu.Visible = false;
		_creditsPopup.Visible = false;
		_loadingScreen.Visible = false;
		
		SetupButtons();
		_ = StartIntroSequence();
	}

	private void EnsureSceneStructure()
	{
		_layer = GetNodeOrNull<CanvasLayer>("CanvasLayer") ?? new CanvasLayer { Name = "CanvasLayer" };
		if (_layer.GetParent() == null) AddChild(_layer);

		_splashScreen = EnsureControl(_layer, "Splash");
		_bootingScreen = EnsureControl(_layer, "Booting");
		_mainMenu = EnsureControl(_layer, "Menu");
		_loadingScreen = EnsureControl(_layer, "Loading");
		
		_shaderRect = _layer.GetNodeOrNull<ColorRect>("CRTShader") ?? new ColorRect { Name = "CRTShader", MouseFilter = Control.MouseFilterEnum.Ignore };
		if (_shaderRect.GetParent() == null) _layer.AddChild(_shaderRect);
		_shaderRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_layer.MoveChild(_shaderRect, _layer.GetChildCount() - 1);

		_bootText = _bootingScreen.GetNodeOrNull<RichTextLabel>("BootText") ?? new RichTextLabel { Name = "BootText" };
		if (_bootText.GetParent() == null) _bootingScreen.AddChild(_bootText);

		_loadingText = _loadingScreen.GetNodeOrNull<RichTextLabel>("LoadingText") ?? new RichTextLabel { Name = "LoadingText" };
		if (_loadingText.GetParent() == null) _loadingScreen.AddChild(_loadingText);

		_buttonContainer = _mainMenu.GetNodeOrNull<VBoxContainer>("Buttons") ?? new VBoxContainer { Name = "Buttons" };
		if (_buttonContainer.GetParent() == null) _mainMenu.AddChild(_buttonContainer);

		_infoPanel = _mainMenu.GetNodeOrNull<Control>("InfoPanel") ?? new Control { Name = "InfoPanel" };
		if (_infoPanel.GetParent() == null) _mainMenu.AddChild(_infoPanel);

		_creditsPopup = _mainMenu.GetNodeOrNull<Control>("CreditsPopup") ?? new Control { Name = "CreditsPopup" };
		if (_creditsPopup.GetParent() == null) _mainMenu.AddChild(_creditsPopup);

		// Element do animacji wyłączania monitora (Bursztynowy dla ochrony oczu)
		_crtOffFlash = _layer.GetNodeOrNull<ColorRect>("CrtOffFlash") ?? new ColorRect { 
			Name = "CrtOffFlash", 
			Color = AMBER, // ZMIANA: Bursztynowy zamiast Białego
			Visible = false,
			MouseFilter = MouseFilterEnum.Ignore
		};
		if (_crtOffFlash.GetParent() == null) _layer.AddChild(_crtOffFlash);
	}

	private void BuildCyberpunkUI()
	{
		AddDynamicBg(_splashScreen, BG_DARK);
		var sLabel = _splashScreen.GetNodeOrNull<Label>("SplashLabel") ?? new Label { Name = "SplashLabel", MouseFilter = MouseFilterEnum.Ignore };
		sLabel.Text = $"{SplashTitle}\n{SplashSubtitle}";
		sLabel.HorizontalAlignment = HorizontalAlignment.Center;
		sLabel.LabelSettings = new LabelSettings { FontSize = 54, FontColor = AMBER, OutlineSize = 2, OutlineColor = Colors.Black };
		if (sLabel.GetParent() == null) _splashScreen.AddChild(sLabel);
		sLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);

		AddDynamicBg(_bootingScreen, Colors.Black);
		_bootText.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 100);
		_bootText.AddThemeFontSizeOverride("normal_font_size", 26);
		_bootText.AddThemeColorOverride("default_color", AMBER);
		_bootText.BbcodeEnabled = true;

		AddDynamicBg(_loadingScreen, Colors.Black);
		_loadingText.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 100);
		_loadingText.AddThemeFontSizeOverride("normal_font_size", 26);
		_loadingText.AddThemeColorOverride("default_color", AMBER);
		_loadingText.BbcodeEnabled = true;

		AddDynamicBg(_mainMenu, BG_DARK);
		CreateDecorations(_mainMenu);

		_infoPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterRight, LayoutPresetMode.Minsize, 100);
		_infoPanel.CustomMinimumSize = new Vector2(512, 450);
		_infoPanel.Position = new Vector2(GetViewportRect().Size.X - 612, 150); 
		AddBgWithFrame(_infoPanel, "InfoBg", new Color(1, 0.65f, 0, 0.02f), AMBER_DIM);
		
		var staticLabel = _infoPanel.GetNodeOrNull<Label>("StaticLabel") ?? new Label { Name = "StaticLabel", MouseFilter = MouseFilterEnum.Ignore };
		staticLabel.Text = StaticInfoText;
		staticLabel.LabelSettings = new LabelSettings { FontSize = 18, FontColor = AMBER_DIM };
		if (staticLabel.GetParent() == null) _infoPanel.AddChild(staticLabel);
		staticLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 30);

		// --- POPUP TWÓRCÓW (POPRAWKA HITBOXA) ---
		_creditsPopup.CustomMinimumSize = new Vector2(550, 380);
		_creditsPopup.Size = _creditsPopup.CustomMinimumSize; 
		_creditsPopup.PivotOffset = _creditsPopup.CustomMinimumSize / 2;
		_creditsPopup.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
		
		AddBgWithFrame(_creditsPopup, "PopupBg", new Color(0, 0, 0, 0.98f), AMBER);
		
		var creditsContent = _creditsPopup.GetNodeOrNull<Label>("CreditsLabel") ?? new Label { Name = "CreditsLabel", MouseFilter = MouseFilterEnum.Ignore };
		creditsContent.Text = CreditsText;
		creditsContent.LabelSettings = new LabelSettings { FontSize = 22, FontColor = AMBER };
		creditsContent.HorizontalAlignment = HorizontalAlignment.Center;
		if (creditsContent.GetParent() == null) _creditsPopup.AddChild(creditsContent);
		creditsContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 50);

		var closeBtn = _creditsPopup.GetNodeOrNull<Button>("CloseBtn") ?? new Button { Name = "CloseBtn", Text = "[ POWRÓT_DO_KONSOLI ]", Flat = true };
		closeBtn.AddThemeFontSizeOverride("font_size", 18);
		closeBtn.AddThemeColorOverride("font_color", AMBER);
		closeBtn.AddThemeColorOverride("font_hover_color", Colors.White);
		closeBtn.MouseFilter = MouseFilterEnum.Stop; 
		if (closeBtn.GetParent() == null) _creditsPopup.AddChild(closeBtn);
		closeBtn.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom, LayoutPresetMode.Minsize, 30);
		
		if (closeBtn.IsConnected("pressed", Callable.From(HideCredits))) closeBtn.Disconnect("pressed", Callable.From(HideCredits));
		closeBtn.Connect("pressed", Callable.From(HideCredits));

		var title = _mainMenu.GetNodeOrNull<Label>("Title") ?? new Label { Name = "Title", MouseFilter = MouseFilterEnum.Ignore };
		title.Text = GameTitle;
		title.LabelSettings = new LabelSettings { FontSize = 100, FontColor = Colors.White, OutlineSize = 8, OutlineColor = AMBER };
		if (title.GetParent() == null) _mainMenu.AddChild(title);
		title.Position = new Vector2(100, 100);

		_buttonContainer.Position = new Vector2(100, 320);
		foreach (var child in _buttonContainer.GetChildren()) child.QueueFree();
		
		for (int i = 0; i < ButtonLabels.Length; i++)
		{
			int index = i;
			var b = new Button { Text = $"[0{i+1}] {ButtonLabels[i]}", Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(400, 60), Flat = true };
			b.AddThemeFontSizeOverride("font_size", 28);
			b.AddThemeColorOverride("font_color", AMBER);
			b.AddThemeColorOverride("font_hover_color", Colors.White);
			_buttonContainer.AddChild(b);
			
			if (index == 0) b.Pressed += () => _on_play_pressed();
			else if (index == 1) b.Pressed += ShowCredits;
			else if (index == 2) b.Pressed += () => OpenWebsite(WebsiteURL);
			else if (index == 3) b.Pressed += () => OpenWebsite(WebsiteURL2); 
			else if (index == ButtonLabels.Length - 1) b.Pressed += () => { PlaySfx(ClickSoundStream); GetTree().Quit(); };
		}
	}

	private void AddBgWithFrame(Control p, string name, Color bgColor, Color? frameColor = null)
	{
		var bg = p.GetNodeOrNull<ColorRect>(name) ?? new ColorRect { Name = name };
		bg.Color = bgColor;
		bg.MouseFilter = MouseFilterEnum.Ignore; 
		if (bg.GetParent() == null) p.AddChild(bg);
		bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		
		var frame = p.GetNodeOrNull<ReferenceRect>("Frame") ?? new ReferenceRect { Name = "Frame", BorderColor = frameColor ?? AMBER_DIM, EditorOnly = false, BorderWidth = 1 };
		if (frame.GetParent() == null) p.AddChild(frame);
		frame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		p.MoveChild(bg, 0);
	}

	private void ShowCredits()
	{
		PlaySfx(ClickSoundStream);
		TriggerGlitch(0.15f, 0.02f); 
		
		_creditsPopup.Visible = true;
		_creditsPopup.ZIndex = 50; 
		_mainMenu.MoveChild(_creditsPopup, _mainMenu.GetChildCount() - 1);
		
		_creditsPopup.Scale = new Vector2(0.95f, 0.95f);
		_creditsPopup.PivotOffset = _creditsPopup.CustomMinimumSize / 2;

		var t = CreateTween().SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
		t.TweenProperty(_creditsPopup, "scale", Vector2.One, 0.3f);
	}

	private void HideCredits()
	{
		PlaySfx(ClickSoundStream);
		TriggerGlitch(0.08f, 0.01f);
		_creditsPopup.Visible = false;
	}

	private void OpenWebsite(string url)
	{
		PlaySfx(ClickSoundStream);
		OS.ShellOpen(url);
	}

	private void SetupAudioPlayers()
	{
		if (_musicPlayer == null) { _musicPlayer = new AudioStreamPlayer { Name = "MusicPlayer" }; AddChild(_musicPlayer); }
		_musicPlayer.Stream = MusicStream;
		_musicPlayer.VolumeDb = -4.4f; 

		if (_sfxPlayer == null) { _sfxPlayer = new AudioStreamPlayer { Name = "SfxPlayer" }; AddChild(_sfxPlayer); }
		if (_bootSfxPlayer == null) { _bootSfxPlayer = new AudioStreamPlayer { Name = "BootSfxPlayer" }; AddChild(_bootSfxPlayer); }
		_bootSfxPlayer.Stream = BootSoundStream;
	}

	private Control EnsureControl(Node p, string n) 
	{ 
		var c = p.GetNodeOrNull<Control>(n) ?? new Control { Name = n }; 
		if (c.GetParent() == null) p.AddChild(c); 
		c.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); 
		return c; 
	}

	private void SetupButtons()
	{
		foreach (var child in _buttonContainer.GetChildren())
		{
			if (child is Button btn)
			{
				btn.MouseEntered += () => { _ = TriggerGlitch(0.03f, 0.005f); PlaySfx(HoverSoundStream); };
				btn.MouseEntered += () => { var t = CreateTween(); t.TweenProperty(btn, "position:x", 15.0f, 0.2f); };
				btn.MouseExited += () => { var t = CreateTween(); t.TweenProperty(btn, "position:x", 0.0f, 0.3f); };
			}
		}
	}

	private async Task StartIntroSequence()
	{
		if (BootSoundStream != null) _bootSfxPlayer.Play();
		
		await Task.Delay(2000); 
		await TriggerGlitch(0.2f, 0.03f); 
		_splashScreen.Visible = false; 
		_bootingScreen.Visible = true;

		_bootText.Text = "";
		foreach (var line in BootLines) 
		{ 
			_bootText.AppendText(line + "\n"); 
			_ = TriggerGlitch(0.02f, 0.005f); 
			await Task.Delay(850); 
		}

		await Task.Delay(1200);
		await TriggerGlitch(0.2f, 0.03f);
		_bootingScreen.Visible = false; 
		_mainMenu.Visible = true;
		
		if (MenuLoadSoundStream != null) PlaySfx(MenuLoadSoundStream);
		if (MusicStream != null) _musicPlayer.Play();
		_ = StartRandomBackgroundGlitches();
	}

	private async Task StartLoadingSequence()
	{
		if (_isLoading) return;
		_isLoading = true;

		GD.Print(">>> SYSTEM: Inicjalizacja ładowania gry...");
		PlaySfx(ClickSoundStream);
		
		_ = TriggerGlitch(0.5f, 0.08f);
		_mainMenu.Visible = false;
		_loadingScreen.Visible = true;
		
		var tween = CreateTween();
		tween.TweenProperty(_musicPlayer, "volume_db", -80.0f, 1.5f);

		_loadingText.Text = "";
		foreach (var line in LoadingLines)
		{
			_loadingText.AppendText(line + "\n");
			_ = TriggerGlitch(0.05f, 0.012f);
			await Task.Delay(600);
		}

		await Task.Delay(1000);
		
		// --- EFEKT WYŁĄCZANIA MONITORA CRT (Poprawiony) ---
		GD.Print(">>> SYSTEM: Wyłączanie monitora...");
		
		// 1. Ukrywamy tekst ładowania, aby nie przebijał przez animację ani po niej
		_loadingText.Visible = false;

		// 2. Zagraj dźwięk wyłączenia
		if (CrtOffSoundStream != null) PlaySfx(CrtOffSoundStream);

		// 3. Konfiguracja overlay błysku (Bursztynowy jest łagodniejszy)
		_crtOffFlash.Visible = true;
		_crtOffFlash.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_crtOffFlash.PivotOffset = GetViewportRect().Size / 2;
		_crtOffFlash.Scale = Vector2.One;
		_layer.MoveChild(_crtOffFlash, _layer.GetChildCount() - 1); 

		var crtTween = CreateTween().SetParallel(false);
		
		// KROK A: Obraz zwęża się do cienkiej poziomej linii
		crtTween.TweenProperty(_crtOffFlash, "scale:y", 0.005f, 0.2f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
		
		// KROK B: Linia zwęża się do kropki na środku i znika
		crtTween.TweenProperty(_crtOffFlash, "scale:x", 0.0f, 0.15f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
		
		await ToSignal(crtTween, "finished");
		
		// 4. Całkowita ciemność przed zmianą sceny
		_crtOffFlash.Visible = false; 
		await Task.Delay(800); // Zwiększona chwila ciemności dla klimatu (0.8s)

		GD.Print($">>> SYSTEM: Przenoszenie do {GameScenePath}...");
		GetTree().ChangeSceneToFile(GameScenePath);
	}

	private async Task TriggerGlitch(float d, float i) { if (_shaderMaterial == null) return; _shaderMaterial.SetShaderParameter(GLITCH_PARAM, i); await Task.Delay((int)(d * 1000)); _shaderMaterial.SetShaderParameter(GLITCH_PARAM, _baseGlitch); }
	private void PlaySfx(AudioStream s) { if (s == null) return; _sfxPlayer.Stream = s; _sfxPlayer.Play(); }
	
	private async Task StartRandomBackgroundGlitches() 
	{ 
		var r = new Random(); 
		while (_mainMenu.Visible && !_isLoading) 
		{ 
			await Task.Delay(r.Next(6000, 12000)); 
			await TriggerGlitch(0.1f, 0.01f); 
		} 
	}
	
	private void AddDynamicBg(Control p, Color c) 
	{ 
		var b = p.GetNodeOrNull<ColorRect>("Bg") ?? new ColorRect { Name = "Bg" };
		b.Color = c;
		if (b.GetParent() == null) p.AddChild(b); 
		b.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); 
		p.MoveChild(b, 0); 
	}

	private void CreateDecorations(Control p) 
	{ 
		var s = p.GetNodeOrNull<ColorRect>("Scanline") ?? new ColorRect { Name = "Scanline", Color = new Color(1, 0.65f, 0, 0.02f), CustomMinimumSize = new Vector2(0, 4) }; 
		if (s.GetParent() == null) p.AddChild(s); 
		s.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide); 
		var t = CreateTween().SetLoops(); 
		t.TweenProperty(s, "position:y", GetViewportRect().Size.Y + 20, 8.0f).From(-20.0f); 
	}

	public void _on_play_pressed() 
	{ 
		_ = StartLoadingSequence();
	}
}
