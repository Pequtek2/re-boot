using Godot;
using System;
using System.Collections.Generic;

public partial class Usuwanie : Node2D
{
	private LineEdit _sqlField;
	private Label _licznikLabel;
	private Panel _popupStart;
	private Panel _popupKoniec;
	private Control _panelMinigry; 
	
	private RichTextLabel _debugLog;
	private Timer _logTimer;
	private bool _zadanieWykonane = false;

	private int _aktualnyIndeks = -1;
	private int _naprawioneBledy = 0;
	private Random _rnd = new Random();

	// REFERENCJA DO SOUND MANAGERA
	private SoundManager _sound;

	public override void _Ready()
	{
		// 0. INICJALIZACJA DŹWIĘKU
		_sound = GetTree().Root.FindChild("SoundManager", true, false) as SoundManager;
		_sound?.PlayBGM("UsuwanieDodawanie.ogg");
		// 1. REFERENCJE UI
		_sqlField = GetNodeOrNull<LineEdit>("CanvasLayer2/Panel/Node2D/LineEdit");
		_debugLog = GetNodeOrNull<RichTextLabel>("CanvasLayer2/Panel2/DebugLog"); 
		_licznikLabel = GetNodeOrNull<Label>("CanvasLayer2/LicznikLabel");
		_popupStart = GetNodeOrNull<Panel>("CanvasLayer2/PopupStart");
		_popupKoniec = GetNodeOrNull<Panel>("CanvasLayer2/PopupKoniec");
		_panelMinigry = GetNodeOrNull<Control>("CanvasLayer2/Panel");

		_logTimer = new Timer();
		_logTimer.WaitTime = 0.15f; 
		_logTimer.Timeout += GenerujLog;
		AddChild(_logTimer);

		if (_sqlField == null || _debugLog == null) return;

		InicjalizujSidebar();
		InitRows(); 

		// 3. PRZYCISKI GŁÓWNE
		var executeBtn = GetNodeOrNull<Button>("CanvasLayer2/Panel/Node2D/ExecuteBtn"); 
		if (executeBtn != null) executeBtn.Pressed += OnExecutePressed;

		var btnStart = _popupStart.GetNodeOrNull<Button>("BtnZrozumialem");
		if (btnStart != null)
		{
			btnStart.Pressed += () => {
				_sound?.PlayByName("mouseclick");
				_popupStart.Visible = false;
				GetTree().Paused = false;
				_logTimer.Start(); 
			};
		}

		if (_popupKoniec != null)
		{
			var btnOk = _popupKoniec.GetNodeOrNull<Button>("BtnOK");
			if (btnOk != null) {
				btnOk.Pressed += async () => {
					_sound?.PlayByName("mouseclick");
					Global.RekordyGotowe = true; 
					var transitioner = this.FindChild("Transitioner", true, false) as Transitioner;
					if (transitioner != null) {
						GetTree().Paused = false;
						await transitioner.ChangeScene("res://Scenes/Kacper/MenuGlowne.tscn", true);
					} else {
						GetTree().Paused = false;
						GetTree().ChangeSceneToFile("res://Scenes/Kacper/MenuGlowne.tscn");
					}
				};
			}
		}

		for (int i = 1; i <= 7; i++)
		{
			var rect = GetNodeOrNull<ColorRect>($"CanvasLayer2/Panel/Wiersz_1/Bgr_Blad_{i}");
			if (rect != null)
			{
				int index = i;
				rect.GuiInput += (ev) => OnRectInput(ev, index);
			}
		}

		GetTree().Paused = true;
		_popupStart.Visible = true;
		_sound?.PlayPopup(); 
		
		_panelMinigry.Visible = false; 
		AktualizujLicznik();
		_debugLog.AppendText("[color=gold]DEBUGGER READY. AWAITING INPUT...[/color]\n");
	}

	private void InicjalizujSidebar()
	{
		var list = GetNodeOrNull<VBoxContainer>("CanvasLayer2/SideBackground/ScrollContainer/SidebarList");
		if (list == null) return;

		foreach (Node grupa in list.GetChildren())
		{
			if (grupa is VBoxContainer vBox)
			{
				var btnHeader = vBox.GetChildOrNull<Button>(0); 
				var tabeleContainer = vBox.GetChildOrNull<VBoxContainer>(1); 

				if (btnHeader != null && tabeleContainer != null)
				{
					tabeleContainer.Visible = false;
					
					// Dodaj [+] jeśli tekst przycisku go nie posiada
					if (!btnHeader.Text.Contains("[+]") && !btnHeader.Text.Contains("[-]"))
					{
						btnHeader.Text = "[+] " + btnHeader.Text;
					}

					btnHeader.Pressed += () => {
						_sound?.PlayByName("mouseclick");
						tabeleContainer.Visible = !tabeleContainer.Visible;
						
						// Zamiana ikonki w zależności od widoczności
						if (tabeleContainer.Visible)
							btnHeader.Text = btnHeader.Text.Replace("[+]", "[-]");
						else
							btnHeader.Text = btnHeader.Text.Replace("[-]", "[+]");
					};

					foreach (Node t in tabeleContainer.GetChildren())
					{
						if (t is Button btnTab)
						{
							btnTab.Pressed += () => {
								_sound?.PlayByName("mouseclick");
								_sqlField.Text = $"SELECT * FROM {btnTab.Text.ToLower().Replace(" ", "_")};";
								if (btnTab.Text.Contains("corrupted_backup_01")) {
									if (!_panelMinigry.Visible) _sound?.PlayPopup();
									_panelMinigry.Visible = true;
								}
							};
						}
					}
				}
			}
		}
	}

	private void GenerujLog()
	{
		if (_zadanieWykonane || _debugLog == null) return;
		string[] kody = { "FATAL_ERR", "STACK_OV", "NULL_REF", "MEM_LEAK", "SQL_INTRUSION" };
		string kolor = (_rnd.Next(2) == 0) ? "gold" : "red";
		_debugLog.AppendText($"[color={kolor}]>[{DateTime.Now.ToString("HH:mm:ss")}] {kody[_rnd.Next(kody.Length)]}: {(_rnd.Next(100, 999))}[/color]\n");
		_debugLog.ScrollToLine(_debugLog.GetLineCount());
	}

	private void OnExecutePressed()
	{
		if (_aktualnyIndeks != -1)
		{
			_sound?.PlayByName("mouseclick");
			NaprawWiersz(_aktualnyIndeks);
			_aktualnyIndeks = -1;
			_sqlField.Text = "";
		}
	}

	private void OnRectInput(InputEvent @event, int index)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed)
		{
			_sound?.PlayByName("mouseclick");
			_aktualnyIndeks = index;
			_sqlField.Text = $"DELETE FROM corrupted_backup_01 WHERE id = {index};";
		}
	}

	private void NaprawWiersz(int index)
	{
		var lblBlad = GetNodeOrNull<Label>($"CanvasLayer2/Panel/Wiersz_1/Lbl_Blad_{index}");
		if (lblBlad != null && lblBlad.Text != "BŁĄD USUNIĘTY")
		{
			_sound?.PlayByName("loadingscreenblip");
			lblBlad.Text = "BŁĄD USUNIĘTY";
			lblBlad.Modulate = new Color(1, 1, 1);
			
			var bgrBlad = GetNodeOrNull<ColorRect>($"CanvasLayer2/Panel/Wiersz_1/Bgr_Blad_{index}");
			if (bgrBlad != null) bgrBlad.Color = new Color(0, 0.4f, 0); 
			
			var lblWart = GetNodeOrNull<Label>($"CanvasLayer2/Panel/wartosci/WRT{index}");
			if (lblWart != null) lblWart.Text = "DELETED";

			_naprawioneBledy++;
			AktualizujLicznik();
			if (_naprawioneBledy >= 7) ZakonczGre();
		}
	}

	private void ZakonczGre()
	{
		_zadanieWykonane = true; 
		_logTimer.Stop();
		_sound?.PlayPopup();
		if (_popupKoniec != null) 
		{
			_popupKoniec.Visible = true;
			_popupKoniec.MoveToFront();
		}
	}

	private void InitRows()
	{
		string[] kodyBledow = { "ERR_CORRUPT", "NULL_PTR", "DB_CRASH", "STACK_OV", "INDEX_LOST", "INV_HEADER", "DATA_LEAK" };
		for (int i = 1; i <= 7; i++)
		{
			var lblStatus = GetNodeOrNull<Label>($"CanvasLayer2/Panel/Wiersz_1/Lbl_Blad_{i}");
			if (lblStatus != null)
			{
				lblStatus.Text = kodyBledow[i-1];
				lblStatus.Modulate = new Color(1, 0.3f, 0.3f); 
			}
		}
	}

	private void AktualizujLicznik() { if (_licznikLabel != null) _licznikLabel.Text = $"NAPRAWIONO: {_naprawioneBledy}/7"; }
}
