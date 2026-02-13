using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Antywirus : Node2D
{
	private Node canvasLayer;
	private Label czasomierzLabel;
	private Label licznikLabel;      
	private Label sciezkaLabel;      
	private Label procentyLabel;
	private Button startButton; 
	private Label ostatniSkanLabel; 

	private Button btnKategoriaSkan;
	private Button btnKategoriaPrywatnosc;
	private Button btnKategoriaStatus;
	private Button btnKategoriaWydajnosc;

	private Control katPrywatnosc;
	private Control katStatus; 
	private Control katWydajnosc;
	private Control popupStart;
	private Control popupKoniec;
	private Control mójBlueScreen;
	private Label zbieranieInfoLabel;

	private TextureProgressBar wydajnoscProgress;
	private Label plikiTempLabel;
	private Label wydajnoscProcentyLabel;
	private Button btnStartOptymalizacja;
	private TextureProgressBar skanerProgress;

	private SoundManager _sound;

	private string[] nazwyPlikow = {
		"kernel32.dll", "ntdll.dll", "user32.dll", "explorer.exe", 
		"svchost.exe", "bootmgr", "wininit.exe", "services.exe",
		"System.Drawing.dll", "mscorlib.dll", "shell32.dll", 
		"drivers/etc/hosts", "config/system", "logs/setup.log",
		"Temp/tmp_092.vdf", "WindowsUpdate.log", "winshlx.dll"
	};

	private List<Control> listaMalloware = new List<Control>();
	private bool czySkanuje = false;
	private bool czyOptymalizuje = false;
	private bool _isGameOver = false; // NOWE: Zabezpieczenie przed wygraną po BSOD
	private int iloscWirusow = 0; 
	private int iloscPlikowTemp = 1240;
	private float pozostałyCzas = 60.0f;
	private double progPopupu = 15.0; 
	private Tween aktualnyTween;
	private Random random = new Random();

	public override void _Ready()
	{
		try {
			_sound = GetTree().Root.FindChild("SoundManager", true, false) as SoundManager;
			_sound?.PlayBGM("Antywirus.ogg");

			canvasLayer = GetNode("UIantywirus/CanvasLayer");

			popupStart = canvasLayer.GetNodeOrNull<Control>("PopupStart");
			popupKoniec = canvasLayer.GetNodeOrNull<Control>("PopupKoniec");
			mójBlueScreen = canvasLayer.GetNodeOrNull<Control>("BlueScreen");
			
			katPrywatnosc = canvasLayer.GetNodeOrNull<Control>("Prywatnosc");
			katStatus = canvasLayer.GetNodeOrNull<Control>("Status");
			katWydajnosc = canvasLayer.GetNodeOrNull<Control>("Wydajnosc");

			btnKategoriaSkan = canvasLayer.GetNodeOrNull<Button>("ColorRect/BtnOchrona");
			btnKategoriaPrywatnosc = canvasLayer.GetNodeOrNull<Button>("ColorRect/BtnPrywatnosc");
			btnKategoriaStatus = canvasLayer.GetNodeOrNull<Button>("ColorRect/BtnStatus");
			btnKategoriaWydajnosc = canvasLayer.GetNodeOrNull<Button>("ColorRect/BtnWydajnosc");

			if (katStatus != null) {
				startButton = katStatus.GetNodeOrNull<Button>("BtnSkanujTeraz");
				if (startButton != null) {
					startButton.Pressed += _on_start_button_pressed;
				}
			}

			skanerProgress = canvasLayer.GetNodeOrNull<TextureProgressBar>("TextureProgressBar");
			if (skanerProgress != null) {
				procentyLabel = skanerProgress.GetNodeOrNull<Label>("ProcentyLabel");
				sciezkaLabel = skanerProgress.GetNodeOrNull<Label>("SciezkaLabel");
				czasomierzLabel = skanerProgress.GetNodeOrNull<Label>("Czasomierz");
				licznikLabel = skanerProgress.GetNodeOrNull<Label>("LicznikWirusow");
				ostatniSkanLabel = skanerProgress.GetNodeOrNull<Label>("OstatniSkan");

				Button btnWlasciwyStart = skanerProgress.GetNodeOrNull<Button>("StartButton");
				if (btnWlasciwyStart != null) {
					btnWlasciwyStart.Pressed += _on_real_scan_start;
				}
			}

			wydajnoscProgress = canvasLayer.GetNodeOrNull<TextureProgressBar>("WydajnoscProgress");
			if (wydajnoscProgress != null) {
				plikiTempLabel = wydajnoscProgress.GetNodeOrNull<Label>("PlikiTempLabel");
				wydajnoscProcentyLabel = wydajnoscProgress.GetNodeOrNull<Label>("WydajnoscProcentyLabel");
				btnStartOptymalizacja = wydajnoscProgress.GetNodeOrNull<Button>("BtnStartOptymalizacja");
				if (btnStartOptymalizacja != null) {
					btnStartOptymalizacja.Pressed += _on_btn_start_optymalizacja_pressed;
				}
			}

			if (mójBlueScreen != null) zbieranieInfoLabel = mójBlueScreen.GetNodeOrNull<Label>("ZbieranieInfo");
			
			InicjalizujSekcjePrywatnosc();
			InicjalizujPopupy();
			ResetujUI();
			
			ZmienKategorie("Status"); 

			if (popupStart != null) {
				popupStart.Visible = true;
				_sound?.PlayPopup();
			}

		} catch (Exception e) { GD.PrintErr("BŁĄD INICJALIZACJI: " + e.Message); }
	}

	public void _on_start_button_pressed() {
		_sound?.PlayByName("mouseclick");
		ZmienKategorie("Skanowanie");
		czySkanuje = false;
	}

	public void _on_real_scan_start() {
		if (czySkanuje || skanerProgress == null) return;
		_sound?.PlayByName("mouseclick");
		czySkanuje = true;
		progPopupu = 15.0;
		if (procentyLabel != null) procentyLabel.Visible = true;
		Button btn = skanerProgress.GetNodeOrNull<Button>("StartButton");
		if (btn != null) btn.Visible = false;

		aktualnyTween = CreateTween();
		aktualnyTween.TweenProperty(skanerProgress, "value", 100.0, 35.0);
	}

	private void _on_btn_start_optymalizacja_pressed()
	{
		if (czyOptymalizuje || wydajnoscProgress == null) return;
		_sound?.PlayByName("mouseclick");
		czyOptymalizuje = true;
		if (btnStartOptymalizacja != null) {
			btnStartOptymalizacja.Disabled = true;
			btnStartOptymalizacja.Text = "OPTYMALIZACJA...";
		}
		Tween t = CreateTween();
		t.TweenProperty(wydajnoscProgress, "value", 100.0, 5.0);
		t.Finished += ZakonczOptymalizacje;
	}

	private void ZakonczOptymalizacje()
	{
		czyOptymalizuje = false;
		iloscPlikowTemp = 0;
		_sound?.PlayPopup();
		if (plikiTempLabel != null) plikiTempLabel.Text = "System czysty!";
		if (btnStartOptymalizacja != null) btnStartOptymalizacja.Text = "UKOŃCZONO";
	}

	public void ZmienKategorie(string nazwa)
	{
		if (katPrywatnosc != null) katPrywatnosc.Visible = (nazwa == "Prywatnosc");
		if (katStatus != null) katStatus.Visible = (nazwa == "Status");
		if (katWydajnosc != null) katWydajnosc.Visible = (nazwa == "Wydajnosc");
		if (wydajnoscProgress != null) wydajnoscProgress.Visible = (nazwa == "Wydajnosc");

		bool pokazSkan = (nazwa == "Skanowanie");
		if (skanerProgress != null) {
			skanerProgress.Visible = pokazSkan;
			if (sciezkaLabel != null) sciezkaLabel.Visible = pokazSkan;
			if (czasomierzLabel != null) czasomierzLabel.Visible = pokazSkan;
			if (licznikLabel != null) licznikLabel.Visible = pokazSkan;
			if (ostatniSkanLabel != null) ostatniSkanLabel.Text = "Ostatni skan: dzisiaj";
			if (ostatniSkanLabel != null) ostatniSkanLabel.Visible = pokazSkan;
			if (procentyLabel != null) procentyLabel.Visible = pokazSkan && czySkanuje;
			Button btn = skanerProgress.GetNodeOrNull<Button>("StartButton");
			if (btn != null) btn.Visible = pokazSkan && !czySkanuje;
		}
		AktualizujWygladPrzyciskow(nazwa);
	}

	public void _on_btn_status_pressed() { _sound?.PlayByName("mouseclick"); ZmienKategorie("Status"); }
	public void _on_btn_ochrona_pressed() { _sound?.PlayByName("mouseclick"); ZmienKategorie("Skanowanie"); }
	public void _on_btn_prywatnosc_pressed() { _sound?.PlayByName("mouseclick"); ZmienKategorie("Prywatnosc"); }
	public void _on_btn_wydajnosc_pressed() { _sound?.PlayByName("mouseclick"); ZmienKategorie("Wydajnosc"); }
	
	public override void _Process(double delta)
	{
		if (czySkanuje) ProcesSkanowania(delta);
		if (czyOptymalizuje) ProcesOptymalizacji();
	}

	private void ProcesSkanowania(double delta)
	{
		if (skanerProgress == null || _isGameOver) return; // Blokada jeśli BlueScreen

		if (pozostałyCzas > 0) {
			pozostałyCzas -= (float)delta;
			if (czasomierzLabel != null) czasomierzLabel.Text = $"Pozostały czas skanowania: {(int)Math.Max(0, pozostałyCzas)}s";
		} else { WlaczBlueScreen(); return; }

		if (procentyLabel != null) procentyLabel.Text = $"{(int)skanerProgress.Value}%";
		if (sciezkaLabel != null && Engine.GetFramesDrawn() % 5 == 0) {
			sciezkaLabel.Text = $"Skanowanie: C:/Windows/{nazwyPlikow[random.Next(nazwyPlikow.Length)]}";
		}
		if (skanerProgress.Value >= progPopupu && progPopupu < 95.0) { PokazWirusa(); progPopupu += 15.0; }
		if (skanerProgress.Value >= 100) ZakonczSkanowanie();
	}

	private void ProcesOptymalizacji()
	{
		if (wydajnoscProcentyLabel != null && wydajnoscProgress != null)
			wydajnoscProcentyLabel.Text = $"{(int)wydajnoscProgress.Value}%";
		if (iloscPlikowTemp > 0 && Engine.GetFramesDrawn() % 5 == 0) {
			iloscPlikowTemp -= 17;
			if (iloscPlikowTemp < 0) iloscPlikowTemp = 0;
			if (plikiTempLabel != null) plikiTempLabel.Text = $"Usuwanie: {iloscPlikowTemp} plików TEMP...";
		}
	}

	private void PokazWirusa() {
		if (aktualnyTween != null) aktualnyTween.Pause();
		_sound?.PlayPopup(); 
		iloscWirusow++;
		if (licznikLabel != null) {
			licznikLabel.Text = $"WYKRYTE WIRUSY: {iloscWirusow}";
			licznikLabel.Modulate = new Color(1, 0, 0);
		}
		if (listaMalloware.Count > 0) {
			var p = listaMalloware[random.Next(listaMalloware.Count)];
			p.Visible = true; p.MoveToFront();
			p.GlobalPosition = new Vector2(random.Next(200, 600), random.Next(100, 400));
		}
	}

	public void _on_wylacz_button_pressed() {
		_sound?.PlayByName("mouseclick");
		foreach (var p in listaMalloware) p.Visible = false;
		if (aktualnyTween != null) aktualnyTween.Play();
	}
	
	public void _on_btn_zrozumialem_pressed() { 
		_sound?.PlayByName("mouseclick");
		if (popupStart != null) popupStart.Visible = false; 
	}

	public void _on_btn_ok_pressed() {
		_sound?.PlayByName("mouseclick");
		RestartDoMenu(); // To wywołuje przycisk OK po wygranej
	}

	private void ZakonczSkanowanie() {
		if (_isGameOver) return; // NOWE: Zabezpieczenie
		czySkanuje = false;
		_sound?.PlayPopup();
		if (popupKoniec != null) {
			popupKoniec.Visible = true;
			popupKoniec.MoveToFront();
		}
	}

	private void WlaczBlueScreen() {
		_isGameOver = true; // NOWE: Blokujemy sukces
		czySkanuje = false; 
		if (aktualnyTween != null) aktualnyTween.Kill();
		_sound?.PlayByName("glitch");

		// Ukrywamy popupy wirusów, żeby nie zostały na BlueScreenie
		foreach (var p in listaMalloware) p.Visible = false;
		if (popupStart != null) popupStart.Visible = false;

		if (mójBlueScreen != null) {
			mójBlueScreen.Visible = true;
			mójBlueScreen.ZIndex = 100; // Wymuszamy wierzch
			Tween t = CreateTween();
			t.TweenMethod(Callable.From<int>((p) => { if (zbieranieInfoLabel != null) zbieranieInfoLabel.Text = $"Postęp: {p}%"; }), 0, 100, 8.0f);
			t.Finished += RestartPoPorazce; // NOWE: Inna funkcja restartu
		}
	}

	private void RestartDoMenu() 
	{
		Global.AntywirusZaliczony = true; // TUTAJ ZALICZAMY
		Global.CzyKolejnaGraOdblokowana = true;
		GetTree().ChangeSceneToFile("res://Scenes/Kacper/MenuGlowne.tscn");
	}

	private void RestartPoPorazce() 
	{
		// TUTAJ NIE ZALICZAMY - po prostu wracamy do menu
		GetTree().ChangeSceneToFile("res://Scenes/Kacper/MenuGlowne.tscn");
	}

	private void InicjalizujSekcjePrywatnosc()
	{
		if (katPrywatnosc == null) return;
		var grid = katPrywatnosc.GetNodeOrNull<GridContainer>("GridContainer");
		Node kontenerPaneli = (grid != null) ? grid : katPrywatnosc;

		foreach (Node panel in kontenerPaneli.GetChildren())
		{
			var btn = panel.GetNodeOrNull<Button>("VBoxContainer/Button");
			var lblStatus = panel.GetNodeOrNull<Label>("VBoxContainer/Label2");
			if (btn != null)
			{
				string nazwaUslugi = panel.Name;
				btn.Pressed += () => ObslugaPrzyciskuPrywatnosci(btn, lblStatus, nazwaUslugi);
			}
		}
	}

	private async void ObslugaPrzyciskuPrywatnosci(Button przycisk, Label status, string nazwa)
	{
		_sound?.PlayByName("mouseclick");
		przycisk.Disabled = true;
		przycisk.Text = "ŁADOWANIE...";
		await ToSignal(GetTree().CreateTimer(1.2f), SceneTreeTimer.SignalName.Timeout);
		_sound?.PlayByName("loadingscreenblip");
		przycisk.Text = "AKTYWNE";
		if (status != null) {
			status.Text = $"System: {nazwa} działa poprawnie.";
			status.Modulate = new Color(0.4f, 1.0f, 0.4f);
		}
	}

	private void InicjalizujPopupy() {
		listaMalloware.Clear();
		foreach (Node child in canvasLayer.GetChildren()) {
			if (child is Control c && c.Name.ToString().Contains("Popup") && c != popupStart && c != popupKoniec) {
				listaMalloware.Add(c);
				c.Visible = false;
			}
		}
	}

	private void ResetujUI() {
		czySkanuje = false;
		_isGameOver = false; // Reset przy restarcie sceny
		if (skanerProgress != null) skanerProgress.Value = 0;
		if (aktualnyTween != null) aktualnyTween.Kill();
		if (ostatniSkanLabel != null) ostatniSkanLabel.Text = "Ostatni skan: dzisiaj";
		if (sciezkaLabel != null) sciezkaLabel.Text = "Gotowy do skanowania...";
	}

	private void AktualizujWygladPrzyciskow(string aktywnaNazwa) {
		var przyciski = new Dictionary<string, Button> {
			{ "Skanowanie", btnKategoriaSkan },
			{ "Prywatnosc", btnKategoriaPrywatnosc },
			{ "Status", btnKategoriaStatus },
			{ "Wydajnosc", btnKategoriaWydajnosc }
		};

		foreach (var para in przyciski) {
			if (para.Value == null) continue;
			if (para.Key == aktywnaNazwa) {
				StyleBoxFlat s = new StyleBoxFlat();
				s.BgColor = new Color(0.4f, 1.0f, 0.4f, 0.4f);
				s.BorderWidthLeft = 5;
				s.BorderColor = new Color(0.4f, 1.0f, 0.4f);
				para.Value.AddThemeStyleboxOverride("normal", s);
			} else {
				para.Value.RemoveThemeStyleboxOverride("normal");
			}
		}
	}
}
