using Godot;
using System;
using System.Collections.Generic;
using System.Linq; 
using System.Threading.Tasks;

public partial class GameManager : Node2D
{
	[Export] public PackedScene ItemPrefab; 
	[Export] public PackedScene SlotPrefab; 
	[Export] public PackedScene MainMenuScene;
	[Export] public string TargetMachineID = "machine_1";

	// --- AUDIO ---
	[Export] public AudioStream SoundClick;
	[Export] public AudioStream SoundSuccess;
	[Export] public AudioStream SoundError;
	[Export] public AudioStream SoundDrop;
	[Export] public AudioStream SoundGameWin;
	[Export] public Godot.Collections.Array<AudioStream> BackgroundMusic;

	// --- INTEGRACJA FILTRU CRT (DODANO) ---
	[Export] public ColorRect ShaderRect; // Tutaj przypiszesz swój ColorRect z shaderem
	private ShaderMaterial _shaderMaterial;
	private float _baseGlitch = 0.0015f; // Podstawowy poziom szumu

	// --- REFERENCJE ---
	private Node2D _itemsContainer;
	private Node2D _slotsContainer;
	private BoardRenderer _boardRenderer;
	
	// --- UI ELEMENTS ---
	private Label _statusLabel;
	private Label _levelLabel;
	private Button _hintButton;
	private Control _endScreen;
	private Control _startScreen;
	private Control _hintWindow;
	private Label _hintText;
	private Control _levelCompleteWindow;
	private Label _levelCompleteText;
	private HBoxContainer _topBar;
	
	// --- SYSTEMY ---
	private AudioStreamPlayer _audioPlayer; 
	private AudioStreamPlayer _musicPlayer; 
	private List<AudioStream> _playlist = new List<AudioStream>();
	private int _currentTrackIndex = 0;

	private CanvasLayer _transitionLayer;
	private List<ColorRect> _transitionPixels = new List<ColorRect>();

	private int _currentLevelIdx = 0;
	private List<Slot> _activeSlots = new List<Slot>();

	// --- KOLORY ---
	private readonly Color C_NEON_BLUE = Color.FromHtml("#00ffcc");
	private readonly Color C_NEON_PINK = Color.FromHtml("#ff0055");
	private readonly Color C_NEON_YELLOW = Color.FromHtml("#ffeb3b");

	// --- WĘŻE (FX) ---
	private class ElectricSnake
	{
		public List<Vector2> Body = new List<Vector2>();
		public Vector2 Head;
		public Vector2 Direction;
		public Color Color;
		public float MoveTimer;
		public float Speed;
	}
	private List<ElectricSnake> _snakes = new List<ElectricSnake>();
	private Random _rng = new Random();

	// --- DANE ---
	private struct ItemData { public string Type; public string Name; public ItemData(string t, string n) { Type = t; Name = n; } }

	public override void _Ready()
	{
		// 1. Audio
		_audioPlayer = new AudioStreamPlayer();
		AddChild(_audioPlayer);
		
		_musicPlayer = new AudioStreamPlayer();
		AddChild(_musicPlayer);
		_musicPlayer.Finished += OnMusicTrackFinished;
		StartBackgroundMusic();

		// 2. Kontenery
		_itemsContainer = GetNode<Node2D>("ItemsContainer");
		_slotsContainer = GetNode<Node2D>("SlotsContainer");
		_boardRenderer = GetNode<BoardRenderer>("BoardRenderer");

		// 3. Setup Shadera (DODANO)
		if (ShaderRect != null && ShaderRect.Material is ShaderMaterial mat)
		{
			_shaderMaterial = mat;
			_shaderMaterial.SetShaderParameter("glitch_intensity", _baseGlitch);
		}

		// 4. Setup reszty
		SetupTransitionLayer();
		CreateInventoryPanel();
		SetupUserInterface();

		GetTree().Root.SizeChanged += OnViewportSizeChanged;
		CallDeferred("OnViewportSizeChanged");

		// 5. Start Gry
		ShowStartScreen();
		InitSnakes();
	}

	public override void _Process(double delta)
	{
		bool showSnakes = (_endScreen != null && _endScreen.Visible) || (_startScreen != null && _startScreen.Visible);
		if (showSnakes)
		{
			UpdateSnakes((float)delta);
			if (_endScreen != null && _endScreen.Visible) _endScreen.QueueRedraw();
			if (_startScreen != null && _startScreen.Visible) _startScreen.QueueRedraw();
		}
	}

	// Metoda do wywoływania efektu zakłóceń (DODANO)
	private async Task TriggerGlitch(float duration, float intensity)
	{
		if (_shaderMaterial == null) return;
		_shaderMaterial.SetShaderParameter("glitch_intensity", intensity);
		await Task.Delay((int)(duration * 1000));
		_shaderMaterial.SetShaderParameter("glitch_intensity", _baseGlitch);
	}

	private void OnDrawSnakes(Control targetCanvas)
	{
		// Ta metoda teraz przyjmuje cel (Control), na którym ma rysować.
		
		foreach (var snake in _snakes)
		{
			if (snake.Body.Count < 2) continue;
			
			// Rysuj linię
			for (int i = 0; i < snake.Body.Count - 1; i++)
			{
				float alpha = (float)i / snake.Body.Count;
				Color c = snake.Color;
				c.A = alpha * 0.5f; 
				targetCanvas.DrawLine(snake.Body[i], snake.Body[i+1], c, 4.0f);
			}
			// Rysuj głowę
			targetCanvas.DrawCircle(snake.Head, 4, snake.Color);
		}
	}

	// ========================================================================
	//                                  METODY POMOCNICZE
	// ========================================================================

	private void PlaySound(AudioStream stream)
	{
		if (stream != null) { _audioPlayer.Stream = stream; _audioPlayer.Play(); }
	}

	private string GetLevelExplanation(int levelIdx)
	{
		switch(levelIdx)
		{
			case 0: return "Obwód został zamknięty! Elektrony mogą teraz swobodnie przepływać od bieguna ujemnego do dodatniego przez żarnik żarówki.";
			case 1: return "Świetna robota! Rezystor (opornik) ograniczył natężenie prądu, chroniąc delikatną diodę LED przed spaleniem.";
			case 2: return "Układ stabilny! Kondensator podłączony równolegle działa jak mały magazyn energii - wygładza nagłe spadki napięcia.";
			case 3: return "Maszyna ruszyła! Bezpiecznik zabezpiecza instalację przed pożarem w razie zwarcia, a włącznik daje kontrolę.";
			default: return "Układ naprawiony.";
		}
	}

	private Button CreateCyberButton(string text, Color color)
	{
		var btn = new Button();
		btn.Text = text;
		ApplyCyberButtonStyle(btn, color);
		return btn;
	}

	private void ApplyCyberButtonStyle(Button btn, Color color)
	{
		btn.Flat = false;
		var styleNormal = new StyleBoxFlat();
		styleNormal.BgColor = new Color(0, 0.1f, 0.15f, 0.9f);
		styleNormal.BorderWidthBottom = 2; styleNormal.BorderWidthTop = 2; styleNormal.BorderWidthLeft = 2; styleNormal.BorderWidthRight = 2;
		styleNormal.BorderColor = color;
		styleNormal.CornerRadiusTopLeft = 0;
		var styleHover = (StyleBoxFlat)styleNormal.Duplicate();
		styleHover.BgColor = new Color(color.R, color.G, color.B, 0.2f);
		btn.AddThemeStyleboxOverride("normal", styleNormal);
		btn.AddThemeStyleboxOverride("hover", styleHover);
		btn.AddThemeStyleboxOverride("pressed", styleHover);
		btn.AddThemeColorOverride("font_color", color);
		btn.AddThemeFontSizeOverride("font_size", 18);
		btn.CustomMinimumSize = new Vector2(180, 40);
	}
	
	private void CenterScreen(Control screen, Vector2 vpSize)
	{
		if (screen != null)
		{
			 screen.Size = vpSize; 
			 screen.Position = Vector2.Zero;
			 var centerContainer = screen.GetNodeOrNull<Control>("CenterContainer");
			 if (centerContainer != null) 
				 centerContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
		}
	}

	// ========================================================================
	//                                  GAME FLOW
	// ========================================================================

	private async void StartGame()
	{
		PlaySound(SoundClick);
		await PlayTransitionEffect(true);

		if (_startScreen != null) _startScreen.Visible = false;
		if (_topBar != null) _topBar.Visible = true;
		
		var invPanel = _itemsContainer.GetNodeOrNull<Panel>("InventoryPanel");
		if (invPanel != null) invPanel.Visible = true;

		LoadLevel(0);
		await PlayTransitionEffect(false);
	}

	private void ShowStartScreen()
	{
		if (_startScreen != null)
		{
			_startScreen.Visible = true;
			if (_topBar != null) _topBar.Visible = false;
			
			var invPanel = _itemsContainer.GetNodeOrNull<Panel>("InventoryPanel");
			if (invPanel != null) invPanel.Visible = false;
		}
	}

	private async void OnNextLevelPressed()
	{
		PlaySound(SoundClick);
		await PlayTransitionEffect(true);
		LoadLevel(_currentLevelIdx + 1);
		await PlayTransitionEffect(false);
	}

	private void LoadLevel(int levelIdx)
	{
		if (levelIdx >= 4) { ShowEndScreen(); return; }

		_currentLevelIdx = levelIdx;
		_boardRenderer.CurrentLevel = levelIdx;
		_boardRenderer.QueueRedraw();
		
		if (_endScreen != null) _endScreen.Visible = false;
		if (_hintWindow != null) _hintWindow.Visible = false;
		if (_levelCompleteWindow != null) _levelCompleteWindow.Visible = false;
		
		_levelLabel.Text = $"POZIOM: {levelIdx + 1}/4";
		_statusLabel.Text = "STAN: AWARIA";
		_statusLabel.LabelSettings.FontColor = C_NEON_PINK;

		foreach (Node child in _itemsContainer.GetChildren()) {
			if (child.Name != "InventoryPanel") child.QueueFree(); 
		}
		foreach (Node child in _slotsContainer.GetChildren()) child.QueueFree();
		_activeSlots.Clear();

		CreateInventoryPanel();
		SetupLevelData(levelIdx);
	}

	private void ShowEndScreen()
	{
		if (_endScreen != null) 
		{ 
			_endScreen.Visible = true; 
			if (_topBar != null) _topBar.Visible = false;
			var invPanel = _itemsContainer.GetNodeOrNull<Panel>("InventoryPanel");
			if (invPanel != null) invPanel.Visible = false;
			if (MainGameManager.Instance != null)
				{
				// 1. Zapisz w globalnym stanie, że maszyna jest naprawiona
				MainGameManager.Instance.SetMachineFixed(TargetMachineID);
				
				// 2. ZAKTUALIZUJ ZADANIE (Zamiast CompleteQuest)
				// Używamy ID: "quest_" + ID_maszyny
				// Cel: "Wróć do Kierownika/Inżyniera po nagrodę"
				string questID = "quest_" + TargetMachineID; 
				QuestManager.Instance.ProgressQuest("quest_naprawa_ramienia", 1);
				
				GD.Print($"Minigra wygrana. Maszyna: {TargetMachineID}, Quest zaktualizowany.");
			}	GD.Print($"SUKCES! Maszyna {TargetMachineID} została naprawiona.");
			}
			PlaySound(SoundGameWin);
			OnViewportSizeChanged();
			InitSnakes();
		}

	private void CheckWinCondition()
	{
		bool allCorrect = true;
		foreach(var slot in _activeSlots)
		{
			if (slot.OccupyingItem == null) { allCorrect = false; break; }
			if (slot.OccupyingItem.ItemType != slot.RequiredType) 
			{ 
				allCorrect = false; 
				PlaySound(SoundError); 
			}
		}

		if (allCorrect)
		{
			_statusLabel.Text = "STAN: SPRAWNY";
			_statusLabel.LabelSettings.FontColor = C_NEON_BLUE;
			PlaySound(SoundSuccess);
			_levelCompleteText.Text = GetLevelExplanation(_currentLevelIdx);
			_levelCompleteWindow.Visible = true;
			_levelCompleteWindow.Scale = new Vector2(0, 0);
			var tween = CreateTween();
			tween.TweenProperty(_levelCompleteWindow, "scale", Vector2.One, 0.5f)
				.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

			// Wywołanie glitcha przy wygranej (DODANO)
			_ = TriggerGlitch(0.4f, 0.05f); 
		}
	}

	// ========================================================================
	//                                  SETUP UI
	// ========================================================================

	private void SetupUserInterface()
	{
		var uiLayer = GetNode<CanvasLayer>("UI");
		
		foreach(Node child in uiLayer.GetChildren()) { 
			if (child is not AudioStreamPlayer) child.QueueFree(); 
		}

		// A. GÓRNY PASEK
		_topBar = new HBoxContainer();
		uiLayer.AddChild(_topBar);
		_topBar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		_topBar.Position = new Vector2(0, 15);
		_topBar.Size = new Vector2(GetViewportRect().Size.X, 60);
		_topBar.Alignment = BoxContainer.AlignmentMode.Center;
		_topBar.AddThemeConstantOverride("separation", 40);

		// 1. Suwak Głośności
		var volBox = new VBoxContainer();
		volBox.Alignment = BoxContainer.AlignmentMode.Center;
		_topBar.AddChild(volBox);

		var volLabel = new Label();
		volLabel.Text = "MUZYKA";
		volLabel.LabelSettings = new LabelSettings() { FontSize = 10, FontColor = C_NEON_BLUE };
		volLabel.HorizontalAlignment = HorizontalAlignment.Center;
		volBox.AddChild(volLabel);

		var volSlider = new HSlider();
		volSlider.CustomMinimumSize = new Vector2(120, 20);
		volSlider.MinValue = -30;
		volSlider.MaxValue = 0;
		volSlider.Value = -10;
		volSlider.ValueChanged += (value) => 
		{
			_musicPlayer.VolumeDb = (float)value;
			if (value <= -30) _musicPlayer.VolumeDb = -80; 
		};
		volBox.AddChild(volSlider);

		// 2. Level Label
		_levelLabel = new Label();
		_levelLabel.LabelSettings = new LabelSettings() { FontSize = 24, FontColor = C_NEON_BLUE, OutlineSize = 4, OutlineColor = Colors.Black };
		_levelLabel.VerticalAlignment = VerticalAlignment.Center;
		_topBar.AddChild(_levelLabel);

		// 3. Hint Button
		_hintButton = CreateCyberButton("? BAZA DANYCH", C_NEON_BLUE);
		_topBar.AddChild(_hintButton);
		_hintButton.Pressed += () => { PlaySound(SoundClick); ToggleHintWindow(); };

		// 4. Status Label
		_statusLabel = new Label();
		_statusLabel.LabelSettings = new LabelSettings() { FontSize = 24, FontColor = C_NEON_PINK, OutlineSize = 4, OutlineColor = Colors.Black };
		_statusLabel.VerticalAlignment = VerticalAlignment.Center;
		_topBar.AddChild(_statusLabel);

		// B. Okna
		CreateHintWindow(uiLayer);
		CreateLevelCompleteWindow(uiLayer);
		CreateEndScreen(uiLayer);
		CreateStartScreen(uiLayer);
	}

	private void CreateInventoryPanel()
	{
		if (_itemsContainer.HasNode("InventoryPanel")) return;

		var invPanel = new Panel();
		invPanel.Name = "InventoryPanel";
		invPanel.MouseFilter = Control.MouseFilterEnum.Ignore; 
		invPanel.Position = new Vector2(0, 450); 

		var style = new StyleBoxFlat();
		style.BgColor = Color.FromHtml("#050a0e"); 
		style.BorderWidthTop = 2; 
		style.BorderColor = C_NEON_BLUE;
		invPanel.AddThemeStyleboxOverride("panel", style);

		_itemsContainer.AddChild(invPanel);
		
		var label = new Label();
		label.Name = "Label";
		label.Text = "MAGAZYN CZĘŚCI";
		label.LabelSettings = new LabelSettings() { FontSize = 16, FontColor = new Color(1, 1, 1, 0.5f) };
		label.Position = new Vector2(10, 5);
		invPanel.AddChild(label);
	}

	private void CreateStartScreen(CanvasLayer parent)
	{
		_startScreen = new Panel();
		_startScreen.Name = "StartScreen";
		parent.AddChild(_startScreen);
		_startScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_startScreen.Visible = false;
		
		// PODPINAMY RYSOWANIE DO METODY, KTÓRA PRZYJMUJE TEN EKRAN JAKO CEL
		_startScreen.Draw += () => OnDrawSnakes(_startScreen); 

		var style = new StyleBoxFlat();
		style.BgColor = new Color(0.02f, 0.02f, 0.05f, 0.98f); 
		_startScreen.AddThemeStyleboxOverride("panel", style);

		var centerContainer = new VBoxContainer();
		centerContainer.Name = "CenterContainer";
		centerContainer.Alignment = BoxContainer.AlignmentMode.Center;
		centerContainer.SetAnchorsPreset(Control.LayoutPreset.Center);
		centerContainer.AddThemeConstantOverride("separation", 20);
		_startScreen.AddChild(centerContainer);

		var title = new Label();
		title.Text = "CYBER-ELEKTRYK";
		title.LabelSettings = new LabelSettings() { FontColor = C_NEON_BLUE, FontSize = 72, OutlineSize = 8, OutlineColor = Colors.Black };
		title.HorizontalAlignment = HorizontalAlignment.Center;
		centerContainer.AddChild(title);

		var subTitle = new Label();
		subTitle.Text = "NAPRAW OBWODY";
		subTitle.LabelSettings = new LabelSettings() { FontColor = C_NEON_PINK, FontSize = 36 };
		subTitle.HorizontalAlignment = HorizontalAlignment.Center;
		centerContainer.AddChild(subTitle);

		var spacer = new Control();
		spacer.CustomMinimumSize = new Vector2(0, 50);
		centerContainer.AddChild(spacer);

		var startBtn = CreateCyberButton("ROZPOCZNIJ", C_NEON_YELLOW);
		startBtn.CustomMinimumSize = new Vector2(250, 70);
		startBtn.AddThemeFontSizeOverride("font_size", 24);
		startBtn.Pressed += StartGame;
		
		var btnContainer = new HBoxContainer();
		btnContainer.Alignment = BoxContainer.AlignmentMode.Center;
		btnContainer.AddChild(startBtn);
		centerContainer.AddChild(btnContainer);
	}

	private void CreateEndScreen(CanvasLayer parent)
	{
		_endScreen = new Panel();
		_endScreen.Name = "EndScreen";
		parent.AddChild(_endScreen);
		_endScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_endScreen.Visible = false;
		
		// PODPINAMY RYSOWANIE
		_endScreen.Draw += () => OnDrawSnakes(_endScreen);

		var style = new StyleBoxFlat();
		style.BgColor = new Color(0.02f, 0.02f, 0.05f, 0.98f); 
		_endScreen.AddThemeStyleboxOverride("panel", style);

		var centerContainer = new VBoxContainer();
		centerContainer.Name = "CenterContainer";
		centerContainer.Alignment = BoxContainer.AlignmentMode.Center;
		centerContainer.SetAnchorsPreset(Control.LayoutPreset.Center);
		centerContainer.AddThemeConstantOverride("separation", 20);
		_endScreen.AddChild(centerContainer);

		var title = new Label();
		title.Text = "MASZYNA NAPRAWIONA";
		title.LabelSettings = new LabelSettings() { FontColor = C_NEON_YELLOW, FontSize = 64, OutlineSize = 8, OutlineColor = C_NEON_PINK };
		title.HorizontalAlignment = HorizontalAlignment.Center;
		centerContainer.AddChild(title);

		var subTitle = new Label();
		subTitle.Text = "KLIKNIJ 'PRZEJDŻ DALEJ' ABY KONTYNUOWAĆ";
		subTitle.LabelSettings = new LabelSettings() { FontColor = C_NEON_BLUE, FontSize = 32 };
		subTitle.HorizontalAlignment = HorizontalAlignment.Center;
		centerContainer.AddChild(subTitle);

		var spacer = new Control();
		spacer.CustomMinimumSize = new Vector2(0, 40);
		centerContainer.AddChild(spacer);
		
		var btnContainer = new HBoxContainer();
		btnContainer.Alignment = BoxContainer.AlignmentMode.Center;
		btnContainer.AddThemeConstantOverride("separation", 20); 
		centerContainer.AddChild(btnContainer);

		// 1. RESTART
		var restartBtn = CreateCyberButton("RESTART", C_NEON_BLUE);
		restartBtn.CustomMinimumSize = new Vector2(200, 60);
		restartBtn.Pressed += () => { PlaySound(SoundClick); GetTree().ReloadCurrentScene(); };
		btnContainer.AddChild(restartBtn);

		// 2. MENU
		var menuBtn = CreateCyberButton("PRZEJDŹ DALEJ", C_NEON_PINK);
		menuBtn.CustomMinimumSize = new Vector2(200, 60);
		menuBtn.Pressed += OnMenuPressed; 
		btnContainer.AddChild(menuBtn);
	}

	private void CreateLevelCompleteWindow(CanvasLayer parent)
	{
		_levelCompleteWindow = new Panel();
		_levelCompleteWindow.Name = "LevelCompleteWindow";
		parent.AddChild(_levelCompleteWindow);
		_levelCompleteWindow.Size = new Vector2(600, 350);
		_levelCompleteWindow.PivotOffset = new Vector2(300, 175); 
		_levelCompleteWindow.Visible = false;
		var style = new StyleBoxFlat();
		style.BgColor = new Color(0, 0.1f, 0.05f, 0.98f);
		style.BorderWidthBottom = 2; style.BorderWidthTop = 2; style.BorderWidthLeft = 2; style.BorderWidthRight = 2;
		style.BorderColor = Colors.Lime;
		_levelCompleteWindow.AddThemeStyleboxOverride("panel", style);
		
		var title = new Label();
		title.Text = "OBWÓD NAPRAWIONY";
		title.LabelSettings = new LabelSettings() { FontColor = Colors.Lime, FontSize = 32, OutlineSize = 4, OutlineColor = Colors.Black };
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.Size = new Vector2(600, 50);
		title.Position = new Vector2(0, 20);
		_levelCompleteWindow.AddChild(title);
		
		_levelCompleteText = new Label();
		_levelCompleteText.LabelSettings = new LabelSettings() { FontColor = Colors.White, FontSize = 20 };
		_levelCompleteText.AutowrapMode = TextServer.AutowrapMode.Word;
		_levelCompleteText.HorizontalAlignment = HorizontalAlignment.Center;
		_levelCompleteText.Size = new Vector2(560, 200);
		_levelCompleteText.Position = new Vector2(20, 80);
		_levelCompleteWindow.AddChild(_levelCompleteText);
		
		var nextBtn = CreateCyberButton("NASTĘPNY UKŁAD >>", C_NEON_BLUE);
		nextBtn.Position = new Vector2(210, 280);
		nextBtn.Pressed += OnNextLevelPressed; 
		_levelCompleteWindow.AddChild(nextBtn);
	}

	private void CreateHintWindow(CanvasLayer parent)
	{
		_hintWindow = new Panel();
		_hintWindow.Name = "HintWindow";
		parent.AddChild(_hintWindow);
		_hintWindow.Size = new Vector2(500, 300);
		_hintWindow.PivotOffset = new Vector2(250, 150);
		_hintWindow.Visible = false;
		var style = new StyleBoxFlat();
		style.BgColor = new Color(0, 0.05f, 0.1f, 0.98f);
		style.BorderWidthBottom = 2; style.BorderWidthTop = 2; style.BorderWidthLeft = 2; style.BorderWidthRight = 2;
		style.BorderColor = C_NEON_BLUE;
		_hintWindow.AddThemeStyleboxOverride("panel", style);
		
		var title = new Label();
		title.Text = "BAZA DANYCH: SCHEMAT";
		title.LabelSettings = new LabelSettings() { FontColor = Colors.Yellow, FontSize = 22 };
		title.Position = new Vector2(20, 15);
		_hintWindow.AddChild(title);
		
		_hintText = new Label();
		_hintText.LabelSettings = new LabelSettings() { FontColor = Colors.White, FontSize = 18 };
		_hintText.AutowrapMode = TextServer.AutowrapMode.Word;
		_hintText.Size = new Vector2(460, 200);
		_hintText.Position = new Vector2(20, 60);
		_hintWindow.AddChild(_hintText);
		
		var closeBtn = CreateCyberButton("ZAMKNIJ", C_NEON_PINK);
		closeBtn.Position = new Vector2(160, 240);
		closeBtn.Pressed += () => { PlaySound(SoundClick); ToggleHintWindow(); };
		_hintWindow.AddChild(closeBtn);
	}

	private void ToggleHintWindow()
	{
		_hintWindow.Visible = !_hintWindow.Visible;
		if (_hintWindow.Visible)
		{
			_hintWindow.Scale = new Vector2(0, 0);
			var tween = CreateTween();
			tween.TweenProperty(_hintWindow, "scale", Vector2.One, 0.3f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		}
	}

	// --- SETUP ITEMS ---

	private void SetupLevelData(int levelIdx)
	{
		string hint = "";
		var itemsToSpawn = new List<ItemData>(); 

		if (levelIdx == 0) 
		{
			hint = "Obwód Prosty:\n1. Bateria to źródło energii.\n2. Prąd potrzebuje zamkniętej pętli (Kabel).\n3. Sznurek jest izolatorem - prąd nie popłynie.";
			SpawnSlot(0, 200, 250, "battery", "ŹRÓDŁO"); 
			SpawnSlot(1, 400, 150, "wire", "ŁĄCZE"); 
			itemsToSpawn.Add(new ItemData("battery", "Bateria"));
			itemsToSpawn.Add(new ItemData("wire", "Przewód"));
			itemsToSpawn.Add(new ItemData("rope", "Sznurek"));
		}
		else if (levelIdx == 1) 
		{
			hint = "Dioda LED:\n1. Przewodzi prąd tylko w jedną stronę.\n2. Zwróć uwagę na kierunek strzałki na symbolu!\n3. Wymaga Ogranicznika, aby się nie spalić.";
			SpawnSlot(0, 300, 300, "resistor", "OGRANICZNIK");
			SpawnSlot(1, 500, 300, "led_right", "EMITER");
			itemsToSpawn.Add(new ItemData("resistor", "Rezystor"));
			itemsToSpawn.Add(new ItemData("led_right", "Dioda LED"));
			itemsToSpawn.Add(new ItemData("led_left", "Dioda LED"));
			itemsToSpawn.Add(new ItemData("nail", "Gwóźdź"));
		}
		else if (levelIdx == 2) 
		{
			hint = "Filtr RC:\n1. Kondensator montujemy RÓWNOLEGLE.\n2. Rezystor szeregowo.";
			SpawnSlot(0, 300, 200, "resistor", "REDUKCJA");
			SpawnSlot(1, 450, 300, "cap", "BUFOR");
			itemsToSpawn.Add(new ItemData("resistor", "Rezystor"));
			itemsToSpawn.Add(new ItemData("cap", "Kondensator"));
			itemsToSpawn.Add(new ItemData("wire", "Zworka"));
			itemsToSpawn.Add(new ItemData("coil", "Cewka"));
			itemsToSpawn.Add(new ItemData("led_right", "Dioda LED")); 
		}
		else if (levelIdx == 3) 
		{
			hint = "Silnik:\n1. Bezpiecznik chroni przed zwarciem.\n2. Włącznik uruchamia maszynę.";
			SpawnSlot(0, 280, 300, "fuse", "OCHRONA");
			SpawnSlot(1, 430, 300, "switch", "STEROWANIE");
			itemsToSpawn.Add(new ItemData("fuse", "Bezpiecznik"));
			itemsToSpawn.Add(new ItemData("switch", "Włącznik"));
			itemsToSpawn.Add(new ItemData("nail", "Gwóźdź"));
			itemsToSpawn.Add(new ItemData("wire", "Drut"));
			itemsToSpawn.Add(new ItemData("resistor", "Rezystor"));
		}
		
		_hintText.Text = hint;
		SpawnShuffledItems(itemsToSpawn);
	}

	private void SpawnShuffledItems(List<ItemData> items)
	{
		var rnd = new Random();
		var shuffled = items.OrderBy(x => rnd.Next()).ToList();
		float startX = 100;
		float gap = 600f / (shuffled.Count > 1 ? shuffled.Count - 1 : 1);
		if (shuffled.Count == 1) startX = 400;

		for (int i = 0; i < shuffled.Count; i++)
		{
			SpawnItem(shuffled[i].Type, shuffled[i].Name, startX + (i * gap), 550);
		}
	}

	private void SpawnSlot(int id, float x, float y, string type, string labelText)
	{
		var slot = SlotPrefab.Instantiate<Slot>();
		slot.SlotId = id;
		slot.RequiredType = type;
		slot.Position = new Vector2(x, y);
		slot.SetLabel(labelText);
		_slotsContainer.AddChild(slot);
		_activeSlots.Add(slot);
	}

	private void SpawnItem(string type, string name, float x, float y)
	{
		var item = ItemPrefab.Instantiate<DraggableItem>();
		item.ItemType = type;
		item.ItemName = name;
		item.Position = new Vector2(x, y);
		item.DragEnded += OnItemDropped;
		_itemsContainer.AddChild(item);
	}

	private void OnItemDropped(DraggableItem item)
	{
		PlaySound(SoundDrop); 
		Slot foundSlot = null;
		foreach (var slot in _activeSlots)
		{
			if (item.GlobalPosition.DistanceTo(slot.GlobalPosition) < 75)
			{
				if (!slot.IsOccupied()) { foundSlot = slot; break; }
			}
		}

		if (foundSlot != null)
		{
			PlaySound(SoundClick); 
			item.GlobalPosition = foundSlot.GlobalPosition;
			item.CurrentSlot = foundSlot;
			foundSlot.OccupyingItem = item;
			CheckWinCondition();
		}
		else item.ReturnToStart();
	}

	// --- OTHER ---

	private void OnMenuPressed()
	{
		PlaySound(SoundClick);
		if (MainMenuScene != null) GetTree().ChangeSceneToPacked(MainMenuScene);
		else GD.Print("Brak sceny menu!");
	}

	private void OnViewportSizeChanged()
	{
		var vpSize = GetViewportRect().Size;
		float designWidth = 800; 
		float offsetX = (vpSize.X - designWidth) / 2;
		if (offsetX < 0) offsetX = 0; 

		Vector2 centerOffset = new Vector2(offsetX, 0);
		
		_boardRenderer.Position = centerOffset;
		_slotsContainer.Position = centerOffset;
		_itemsContainer.Position = centerOffset;

		var invPanel = _itemsContainer.GetNodeOrNull<Panel>("InventoryPanel");
		if (invPanel != null)
		{
			invPanel.Position = new Vector2(-offsetX, 450); 
			invPanel.Size = new Vector2(vpSize.X, 200); 
			var label = invPanel.GetNodeOrNull<Label>("Label");
			if (label != null) label.Position = new Vector2(20 + offsetX, 10);
		}

		if (_hintWindow != null) _hintWindow.Position = new Vector2(vpSize.X/2 - 250, vpSize.Y/2 - 150);
		if (_levelCompleteWindow != null) _levelCompleteWindow.Position = new Vector2(vpSize.X/2 - 300, vpSize.Y/2 - 175);
		
		CenterScreen(_endScreen, vpSize);
		CenterScreen(_startScreen, vpSize);

		RegenerateTransitionPixels(); 
		_boardRenderer.QueueRedraw();
	}

	// --- RETRO TRANSITION ---

	private void SetupTransitionLayer()
	{
		_transitionLayer = new CanvasLayer();
		_transitionLayer.Layer = 100;
		AddChild(_transitionLayer);
	}

	private void RegenerateTransitionPixels()
	{
		foreach(var p in _transitionPixels) p.QueueFree();
		_transitionPixels.Clear();

		var vpSize = GetViewportRect().Size;
		float blockSize = 40.0f; 
		int cols = Mathf.CeilToInt(vpSize.X / blockSize);
		int rows = Mathf.CeilToInt(vpSize.Y / blockSize);

		for(int y = 0; y < rows; y++)
		{
			for(int x = 0; x < cols; x++)
			{
				var rect = new ColorRect();
				rect.Size = new Vector2(blockSize, blockSize);
				rect.Position = new Vector2(x * blockSize, y * blockSize);
				rect.Color = Colors.Black;
				rect.Visible = false; 
				rect.PivotOffset = new Vector2(blockSize/2, blockSize/2); 
				_transitionLayer.AddChild(rect);
				_transitionPixels.Add(rect);
			}
		}
	}

	private async Task PlayTransitionEffect(bool appear)
	{
		var rng = new Random();
		var shuffled = _transitionPixels.OrderBy(x => rng.Next()).ToList();
		var tween = CreateTween();
		tween.SetParallel(true);
		int i = 0;
		int total = shuffled.Count;
		float duration = 0.6f; 
		foreach(var p in shuffled)
		{
			float delay = ((float)i / total) * duration;
			if (appear)
			{
				p.Visible = true;
				p.Scale = Vector2.Zero;
				tween.TweenProperty(p, "scale", Vector2.One, 0.2f).SetDelay(delay);
			}
			else tween.TweenProperty(p, "scale", Vector2.Zero, 0.2f).SetDelay(delay);
			i++;
		}
		await ToSignal(tween, "finished");
		if (!appear) foreach(var p in _transitionPixels) p.Visible = false;
	}

	// --- WĘŻE ---

	private void InitSnakes()
	{
		_snakes.Clear();
		var size = GetViewportRect().Size;
		int snakeCount = 15; 
		for (int i = 0; i < snakeCount; i++)
		{
			var snake = new ElectricSnake();
			snake.Head = new Vector2(_rng.Next(0, (int)size.X), _rng.Next(0, (int)size.Y));
			snake.Body.Add(snake.Head);
			snake.Direction = GetRandomDirection();
			snake.Color = _rng.Next(2) == 0 ? C_NEON_BLUE : C_NEON_PINK;
			if (_rng.Next(5) == 0) snake.Color = C_NEON_YELLOW; 
			snake.Speed = 0.05f + (float)_rng.NextDouble() * 0.05f; 
			_snakes.Add(snake);
		}
	}

	private Vector2 GetRandomDirection()
	{
		int d = _rng.Next(4);
		if (d == 0) return new Vector2(1, 0);
		if (d == 1) return new Vector2(-1, 0);
		if (d == 2) return new Vector2(0, 1);
		return new Vector2(0, -1);
	}

	private void UpdateSnakes(float delta)
	{
		var size = GetViewportRect().Size;
		foreach (var snake in _snakes)
		{
			snake.MoveTimer += delta;
			if (snake.MoveTimer > snake.Speed)
			{
				snake.MoveTimer = 0;
				snake.Head += snake.Direction * 20; 
				snake.Body.Add(snake.Head);
				if (snake.Body.Count > 20) snake.Body.RemoveAt(0);
				if (_rng.Next(10) == 0) snake.Direction = GetRandomDirection();
				if (snake.Head.X < 0 || snake.Head.X > size.X || snake.Head.Y < 0 || snake.Head.Y > size.Y)
				{
					snake.Head = new Vector2(_rng.Next(0, (int)size.X), _rng.Next(0, (int)size.Y));
					snake.Body.Clear(); 
					snake.Body.Add(snake.Head);
					snake.Direction = GetRandomDirection();
				}
			}
		}
	}

	private void StartBackgroundMusic()
	{
		if (BackgroundMusic == null || BackgroundMusic.Count == 0) return;
		_playlist = new List<AudioStream>(BackgroundMusic);
		_playlist = _playlist.OrderBy(x => _rng.Next()).ToList();
		_currentTrackIndex = 0;
		PlayNextTrack();
	}

	private void PlayNextTrack()
	{
		if (_playlist.Count == 0) return;
		_musicPlayer.Stream = _playlist[_currentTrackIndex];
		_musicPlayer.Play();
	}   

	private void OnMusicTrackFinished()
	{
		_currentTrackIndex++;
		if (_currentTrackIndex >= _playlist.Count)
		{
			_currentTrackIndex = 0;
			_playlist = _playlist.OrderBy(x => _rng.Next()).ToList();
		}
		PlayNextTrack();
	}
}
