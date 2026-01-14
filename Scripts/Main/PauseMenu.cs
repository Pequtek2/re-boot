using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	[Export] public Button ResumeButton;
	[Export] public Button SettingsButton; // Nowy guzik "Ustawienia"
	[Export] public Button QuitButton;
	[Export] public Control MainMenuContainer; // Kontener z guzikami głównymi

	[Export] public Control SettingsContainer; // Kontener z ustawieniami (domyślnie ukryty)
	[Export] public Button GenderButton;       // Guzik zmiany płci
	[Export] public Button BackButton;         // Guzik powrotu

	private bool _isPaused = false;

	public override void _Ready()
	{
		Visible = false;

		// --- FIX AUTOMATYCZNY ---
		// Jeśli zapomniałeś przypisać w Inspektorze, kod spróbuje znaleźć je po nazwie
		if (MainMenuContainer == null) MainMenuContainer = GetNodeOrNull<Control>("MainMenuContainer");
		if (SettingsContainer == null) SettingsContainer = GetNodeOrNull<Control>("SettingsContainer");
		// ------------------------

		if (SettingsContainer != null) SettingsContainer.Visible = false;
		if (MainMenuContainer != null) MainMenuContainer.Visible = true;

		if (ResumeButton != null) ResumeButton.Pressed += TogglePause;
		if (QuitButton != null) QuitButton.Pressed += () => GetTree().Quit();
		
		if (SettingsButton != null) SettingsButton.Pressed += OpenSettings;
		
		if (GenderButton != null) 
		{
			GenderButton.Pressed += ToggleGender;
			UpdateGenderText(); 
		}
		if (BackButton != null) BackButton.Pressed += CloseSettings;
	}

	// Wklej też poprawioną funkcję TogglePause, żeby nie wywalała błędu
	private void TogglePause()
	{
		_isPaused = !_isPaused;
		GetTree().Paused = _isPaused; 
		Visible = _isPaused;

		if (_isPaused)
		{
			// Jeśli po automatycznym fixie nadal jest null, wypisz błąd, ale nie wyłączaj gry
			if (MainMenuContainer != null) MainMenuContainer.Visible = true;
			else GD.PrintErr("CRITICAL: MainMenuContainer nie znaleziony! Sprawdź nazwy węzłów w scenie PauseMenu.");

			if (SettingsContainer != null) SettingsContainer.Visible = false;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel")) // Klawisz ESC
		{
			// Jeśli jesteśmy w ustawieniach -> cofnij do głównego menu pauzy
			if (Visible && SettingsContainer.Visible)
			{
				CloseSettings();
			}
			// W przeciwnym razie -> pauzuj/odpauzuj grę
			else
			{
				TogglePause();
			}
		}
	}

	private void OpenSettings()
	{
		MainMenuContainer.Visible = false;
		SettingsContainer.Visible = true;
	}

	private void CloseSettings()
	{
		SettingsContainer.Visible = false;
		MainMenuContainer.Visible = true;
	}

	private void ToggleGender()
	{
		if (MainGameManager.Instance == null) return;

		bool newState = !MainGameManager.Instance.IsMale;
		MainGameManager.Instance.SetGender(newState);
		
		UpdateGenderText();
	}

	private void UpdateGenderText()
	{
		if (MainGameManager.Instance == null || GenderButton == null) return;
		string txt = MainGameManager.Instance.IsMale ? "Płeć: Mężczyzna" : "Płeć: Kobieta";
		GenderButton.Text = txt;
	}
}
