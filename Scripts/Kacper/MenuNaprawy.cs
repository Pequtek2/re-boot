using Godot;
using System;
using System.Threading.Tasks;

public partial class MenuNaprawy : Control
{
	private Button _btnAntywirus, _btnTabele, _btnDane, _btnUstawienia, _btnWyjdz;
	private Control _panelVBox, _panelUstawienia;
	private RichTextLabel _labelProtokol; 
	private SoundManager _sound;

	// DODANE: Zmienna eksportowana zgodnie ze screenem
	

	public override async void _Ready()
	{
		_sound = GetTree().Root.FindChild("SoundManager", true, false) as SoundManager;
		_sound?.PlayBGM("BGmenu.ogg");

		_panelVBox = GetNodeOrNull<Control>("VBox");
		_panelUstawienia = GetNodeOrNull<Control>("UstawieniaPanel");
		_labelProtokol = GetNodeOrNull<RichTextLabel>("RichTextLabel");

		if (_panelUstawienia != null) _panelUstawienia.Visible = false;

		_btnAntywirus = GetNodeOrNull<Button>("VBox/SkanowanieWirusow/BtnKable");
		_btnTabele = GetNodeOrNull<Button>("VBox/UsuniecieZepsutychDanych/BtnTabele");
		_btnDane = GetNodeOrNull<Button>("VBox/WprowadzeniePonownieDanych/BtnDane");
		_btnUstawienia = GetNodeOrNull<Button>("VBox/Ustawienia/BtnUstawienia");
		_btnWyjdz = GetNodeOrNull<Button>("VBox/Wyjscie/BtnWyjdz");

		if (_btnWyjdz != null) _btnWyjdz.Visible = false;

		InicjalizujSuwaki();
		await WykonajSekwencjeStartowa();
		
		SprawdzCzyMoznaWylogowac();
	}

	private void SprawdzCzyMoznaWylogowac()
		{
			if (Global.AntywirusZaliczony && Global.RekordyGotowe && Global.DaneGotowe)
		{
			string currentID = Global.CurrentMachineID; 

			if (MainGameManager.Instance != null)
			{
				MainGameManager.Instance.SetMachineFixed(currentID);
				QuestManager.Instance.ProgressQuest("main_quest_3", 1);
				QuestManager.Instance.ProgressQuest("story_main", 1);
				TagManager.Instance.AddTag("machine_3_fixed");
				// Log od Adama nr 1:
				GD.Print($"Minigra wygrana. Maszyna: {currentID}, Quest zaktualizowany.");
			}
	
			if (_btnWyjdz != null && !_btnWyjdz.Visible) 
			{
				_btnWyjdz.Visible = true;
				// Dodajemy animację pisania, żeby "WYLOGUJ" nie wskoczyło nagle
				_ = WypiszTekst(_btnWyjdz, "> WYLOGUJ"); 
			}

			// Log od Adama nr 2:
			GD.Print($"SUKCES! Maszyna {currentID} została naprawiona.");
		}
	}

	private void InicjalizujSuwaki()
	{
		if (_panelUstawienia == null) return;
		var musicSlider = _panelUstawienia.GetNodeOrNull<HSlider>("VBoxContainer/MusicSlider");
		var sfxSlider = _panelUstawienia.GetNodeOrNull<HSlider>("VBoxContainer/SFXSlider");
		var btnPowrot = _panelUstawienia.GetNodeOrNull<Button>("VBoxContainer/BtnPowrot");

		if (musicSlider != null) {
			musicSlider.Value = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Music"))) * 100;
			musicSlider.ValueChanged += (v) => _sound?.SetVolume("Music", (float)v / 100f);
		}
		if (sfxSlider != null) {
			sfxSlider.Value = Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("SFX"))) * 100;
			sfxSlider.ValueChanged += (v) => _sound?.SetVolume("SFX", (float)v / 100f);
		}
		if (btnPowrot != null) btnPowrot.Pressed += _on_btn_powrot_pressed;
	}

	private async Task WykonajSekwencjeStartowa() 
	{
		if (_btnAntywirus != null) {
			string status = Global.AntywirusZaliczony ? "[V]" : "[ ]";
			if (Global.AntywirusZaliczony) _btnAntywirus.Modulate = new Color(0, 1, 0); 
			await WypiszTekst(_btnAntywirus, $"> SKANUJ SYSTEM {status}");
		}

		if (_btnTabele != null) {
			if (Global.AntywirusZaliczony) {
				_btnTabele.Disabled = false;
				string status = Global.RekordyGotowe ? "[V]" : "[ ]";
				if (Global.RekordyGotowe) _btnTabele.Modulate = new Color(0, 1, 0);
				await WypiszTekst(_btnTabele, $"> OCZYŚĆ TABELE {status}");
			} else {
				_btnTabele.Disabled = true;
				await WypiszTekst(_btnTabele, "> [ZABLOKOWANE] [ ]");
			}
		}

		if (_btnDane != null) {
			if (Global.RekordyGotowe) {
				_btnDane.Disabled = false;
				string status = Global.DaneGotowe ? "[V]" : "[ ]";
				if (Global.DaneGotowe) _btnDane.Modulate = new Color(0, 1, 0);
				await WypiszTekst(_btnDane, $"> ZAPISZ DANE {status}");
			} else {
				_btnDane.Disabled = true;
				await WypiszTekst(_btnDane, "> [ZABLOKOWANE] [ ]");
			}
		}

		if (_btnUstawienia != null) await WypiszTekst(_btnUstawienia, "> USTAWIENIA");
	}

	private async Task WypiszTekst(Button btn, string txt) 
	{
		btn.Text = ""; 
		_sound?.StartTypingSound();
		foreach (char c in txt) { 
			if(!IsInsideTree()) break; 
			btn.Text += c; 
			await ToSignal(GetTree().CreateTimer(0.015f), SceneTreeTimer.SignalName.Timeout); 
		}
		_sound?.StopTypingSound();
	}

	private void _on_btn_ustawienia_pressed() { _sound?.PlayByName("mouseclick"); _panelVBox.Visible = false; _panelUstawienia.Visible = true; _labelProtokol.Visible = false; }
	private void _on_btn_powrot_pressed() { _sound?.PlayByName("mouseclick"); _panelUstawienia.Visible = false; _panelVBox.Visible = true; _labelProtokol.Visible = true; }
	private async void _on_btn_kable_pressed() { _sound?.PlayByName("mouseclick"); await WywolajPrzejscie("res://Scenes/Kacper/Antywirus.tscn"); }
	private async void _on_btn_tabele_pressed() { if(Global.AntywirusZaliczony) { _sound?.PlayByName("mouseclick"); await WywolajPrzejscie("res://Scenes/Kacper/UsuniecieRekordow.tscn"); }}
	private async void _on_btn_dane_pressed() { if(Global.RekordyGotowe) { _sound?.PlayByName("mouseclick"); await WywolajPrzejscie("res://Scenes/Kacper/WprowadzenieDanych.tscn"); }}
	private async void _on_btn_wyjdz_pressed() { _sound?.PlayByName("mouseclick"); await WywolajPrzejscie("res://Scenes/Main/FactoryHub.tscn"); }
	private async Task WywolajPrzejscie(string path) { var trans = GetTree().Root.FindChild("Transitioner", true, false) as Transitioner; if (trans != null) await trans.ChangeScene(path, true); else GetTree().ChangeSceneToFile(path); }
}
