using Godot;
using System;
using System.Collections.Generic;

public partial class WprowadzenieDanych : Node2D
{
	private LineEdit _sqlField;
	private Label _licznikLabel;
	private Panel _popupStart, _popupKoniec;
	private Control _panelInsert; 

	private RichTextLabel _debugLog;
	private Timer _logTimer;
	private bool _zadanieWykonane = false;

	private int _aktualnyIndeks = -1;
	private int _wgraneDane = 0;
	private Random _rnd = new Random();

	private SoundManager _sound;

	public override void _Ready()
	{
		_sound = GetTree().Root.FindChild("SoundManager", true, false) as SoundManager;
		_sound?.PlayBGM("UsuwanieDodawanie.ogg");
		
		_sqlField = GetNodeOrNull<LineEdit>("CanvasLayer2/Panel/Node2D/LineEdit");
		_debugLog = GetNodeOrNull<RichTextLabel>("CanvasLayer2/Panel2/DebugLog"); 
		_licznikLabel = GetNodeOrNull<Label>("CanvasLayer2/LicznikLabel");
		_popupStart = GetNodeOrNull<Panel>("CanvasLayer2/PopupStart");
		_popupKoniec = GetNodeOrNull<Panel>("CanvasLayer2/PopupKoniec");
		_panelInsert = GetNodeOrNull<Control>("CanvasLayer2/Panel");

		_logTimer = new Timer();
		_logTimer.WaitTime = 0.15f; 
		_logTimer.Timeout += GenerujLog;
		AddChild(_logTimer);

		if (_panelInsert != null) _panelInsert.Visible = false;
		InicjalizujSidebar();
		InitRows();

		var btnZrozumialem = GetNodeOrNull<Button>("CanvasLayer2/PopupStart/BtnZrozumialem");
		if (btnZrozumialem != null) {
			btnZrozumialem.Pressed += () => {
				_sound?.PlayByName("mouseclick");
				_popupStart.Visible = false;
				GetTree().Paused = false;
				_logTimer.Start();
			};
		}

		var executeBtn = GetNodeOrNull<Button>("CanvasLayer2/Panel/Node2D/ExecuteBtn");
		if (executeBtn != null) executeBtn.Pressed += OnUploadPressed;

		GetTree().Paused = true;
		if (_popupStart != null) 
		{
			_popupStart.Visible = true;
			_sound?.PlayPopup();
		}
		
		AktualizujLicznik();
		if (_debugLog != null) _debugLog.AppendText("[color=gold]DATABASE CONNECTION ESTABLISHED...[/color]\n");
	}

	private void GenerujLog()
	{
		if (_zadanieWykonane || _debugLog == null) return;
		string[] kody = { "MISSING_SECTOR", "DATA_GAP", "BUFFER_EMPTY", "SYNC_LOST", "IO_PENDING" };
		string kolor = (_rnd.Next(2) == 0) ? "gold" : "red";
		_debugLog.AppendText($"[color={kolor}]>[{DateTime.Now.ToString("HH:mm:ss")}] {kody[_rnd.Next(kody.Length)]}: {(_rnd.Next(100, 999))}[/color]\n");
		_debugLog.ScrollToLine(_debugLog.GetLineCount());
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
					if (!btnHeader.Text.Contains("[+]") && !btnHeader.Text.Contains("[-]"))
						btnHeader.Text = "[+] " + btnHeader.Text;

					btnHeader.Pressed += () => {
						_sound?.PlayByName("mouseclick");
						tabeleContainer.Visible = !tabeleContainer.Visible;
						btnHeader.Text = tabeleContainer.Visible ? btnHeader.Text.Replace("[+]", "[-]") : btnHeader.Text.Replace("[-]", "[+]");
					};

					foreach (Node t in tabeleContainer.GetChildren())
					{
						if (t is Button btnTab) {
							btnTab.Pressed += () => {
								_sound?.PlayByName("mouseclick");
								string nazwaTabeli = btnTab.Text.ToLower();
								_sqlField.Text = $"SELECT * FROM {nazwaTabeli.Replace(" ", "_")};";
								_panelInsert.Visible = nazwaTabeli.Contains("emergency_restore_point");
								if (_panelInsert.Visible) _sound?.PlayPopup();
							};
						}
					}
				}
			}
		}
	}

	private void InitRows()
	{
		for (int i = 1; i <= 7; i++)
		{
			var rectStatus = GetNodeOrNull<ColorRect>($"CanvasLayer2/Panel/Wiersz_1/Bgr_Blad_{i}");
			var lblStatus = GetNodeOrNull<Label>($"CanvasLayer2/Panel/Wiersz_1/Lbl_Blad_{i}");
			var lblWrt = GetNodeOrNull<Label>($"CanvasLayer2/Panel/wartosci/WRT{i}");

			if (rectStatus != null) {
				int index = i;
				rectStatus.GuiInput += (ev) => OnRowInput(ev, index);
			}
			
			if (lblStatus != null) {
				lblStatus.Text = "[ BRAK DANYCH ]";
				lblStatus.Modulate = new Color(0.5f, 0.5f, 0.5f);
			}
			if (lblWrt != null) lblWrt.Text = "---";
		}
	}

	private void OnRowInput(InputEvent @event, int index)
	{
		if (@event is InputEventMouseButton mb && mb.Pressed)
		{
			_sound?.PlayByName("mouseclick");
			_aktualnyIndeks = index;
			string hexVal = "0x" + _rnd.Next(0x1000, 0xFFFF).ToString("X");
			_sqlField.Text = $"INSERT INTO emergency_restore_point (id, hash) VALUES ({index}, '{hexVal}');";
		}
	}

	private void OnUploadPressed()
	{
		if (_aktualnyIndeks != -1) {
			_sound?.PlayByName("mouseclick");
			WstawDane(_aktualnyIndeks);
			_aktualnyIndeks = -1;
			_sqlField.Text = "";
		}
	}

	private void WstawDane(int index)
	{
		var lblStatus = GetNodeOrNull<Label>($"CanvasLayer2/Panel/Wiersz_1/Lbl_Blad_{index}");
		var bgrStatus = GetNodeOrNull<ColorRect>($"CanvasLayer2/Panel/Wiersz_1/Bgr_Blad_{index}");
		var lblWrt = GetNodeOrNull<Label>($"CanvasLayer2/Panel/wartosci/WRT{index}");
		var bgrWrt = GetNodeOrNull<ColorRect>($"CanvasLayer2/Panel/wartosci/Bgr_WRT_{index}");

		if (lblStatus != null && lblStatus.Text != "[ WGRANO ]") {
			_sound?.PlayByName("loadingscreenblip");
			_debugLog?.AppendText($"[color=green]>>> SUCCESS: NEW RESTORE POINT AT SECTOR {index}.[/color]\n");
			_debugLog?.ScrollToLine(_debugLog.GetLineCount());

			lblStatus.Text = "[ WGRANO ]";
			lblStatus.Modulate = new Color(1, 1, 1);
			if (bgrStatus != null) bgrStatus.Color = new Color(0, 0.4f, 0.6f); 
			
			if (lblWrt != null) lblWrt.Text = "0x" + _rnd.Next(0x1000, 0xFFFF).ToString("X");
			if (bgrWrt != null) bgrWrt.Color = new Color(0, 0.5f, 0); 

			_wgraneDane++;
			AktualizujLicznik();
			if (_wgraneDane >= 7) KoniecGry();
		}
	}

	private void AktualizujLicznik() => _licznikLabel.Text = $"WGRANO DANYCH: {_wgraneDane}/7";

	private void KoniecGry()
	{
		_zadanieWykonane = true;
		_logTimer.Stop();
		_sound?.PlayPopup();
		_debugLog?.AppendText("[color=green]\n>>> ALL DATA COMMITTED. SYSTEM SECURED.[/color]");

		if (_popupKoniec != null) {
			_popupKoniec.Visible = true;
			_popupKoniec.MoveToFront();
			var btnOk = _popupKoniec.GetNodeOrNull<Button>("BtnOK");
			if (btnOk != null) {
				if (btnOk.IsConnected("pressed", Callable.From(OnBtnOkPressed))) btnOk.Disconnect("pressed", Callable.From(OnBtnOkPressed));
				btnOk.Pressed += OnBtnOkPressed;
			}
		}
	}

	private async void OnBtnOkPressed()
	{
		_sound?.PlayByName("mouseclick");
		Global.DaneGotowe = true; 
		GetTree().Paused = false; 

		string path = "res://Scenes/Kacper/MenuGlowne.tscn";
		var transitioner = GetTree().Root.FindChild("Transitioner", true, false) as Transitioner;
		if (transitioner != null) await transitioner.ChangeScene(path, true);
		else GetTree().ChangeSceneToFile(path);
	}
}
