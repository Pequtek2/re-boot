using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	[Export] public TextureButton ResumeButton;
	[Export] public TextureButton SettingsButton; 
	[Export] public TextureButton QuitButton;
	[Export] public Control MainMenuContainer; 

	[Export] public Control SettingsContainer; 
	[Export] public TextureButton GenderButton;   
	[Export] public Label GednerLabel;    
	[Export] public TextureButton BackButton;         

	[ExportGroup("Audio Settings")]
	[Export] public HSlider VolumeSlider;
	[Export] public Label VolumeLabel;

	private bool _isPaused = false;
	private int _musicBusIndex;

	public override void _Ready()
	{
		Visible = false;

		_musicBusIndex = AudioServer.GetBusIndex("Music");
		if (_musicBusIndex == -1) _musicBusIndex = 0; 

		if (MainMenuContainer == null) MainMenuContainer = GetNodeOrNull<Control>("MainMenuContainer");
		if (SettingsContainer == null) SettingsContainer = GetNodeOrNull<Control>("SettingsContainer");

		if (SettingsContainer != null) SettingsContainer.Visible = false;
		if (MainMenuContainer != null) MainMenuContainer.Visible = true;

		if (ResumeButton != null) ResumeButton.Pressed += TogglePause;
		if (QuitButton != null) QuitButton.Pressed += () => GetTree().Quit();
		if (SettingsButton != null) SettingsButton.Pressed += OpenSettings;
		if (BackButton != null) BackButton.Pressed += CloseSettings;
		if (GenderButton != null) GenderButton.Pressed += ToggleGender;

		SetupVolumeSlider();
	}

	private void SetupVolumeSlider()
	{
		if (VolumeSlider == null && SettingsContainer != null)
			VolumeSlider = SettingsContainer.GetNodeOrNull<HSlider>("VolumeSlider");
		
		if (VolumeLabel == null && SettingsContainer != null)
			VolumeLabel = SettingsContainer.GetNodeOrNull<Label>("VolumeLabel");

		if (VolumeSlider != null)
		{
			VolumeSlider.MinValue = 0.0001;
			VolumeSlider.MaxValue = 1.0;
			VolumeSlider.Step = 0.01;

			float currentVolumeDb = AudioServer.GetBusVolumeDb(_musicBusIndex);
			VolumeSlider.Value = Mathf.DbToLinear(currentVolumeDb);
			
			UpdateVolumeLabel((float)VolumeSlider.Value);

			if (VolumeSlider.IsConnected("value_changed", Callable.From<double>(OnVolumeChanged)))
				VolumeSlider.Disconnect("value_changed", Callable.From<double>(OnVolumeChanged));
			
			VolumeSlider.Connect("value_changed", Callable.From<double>(OnVolumeChanged));
			
			// Usunięto całą stylizację - suwak będzie miał całkowicie domyślny wygląd!
		}
	}

	private void OnVolumeChanged(double value)
	{
		AudioServer.SetBusVolumeDb(_musicBusIndex, Mathf.LinearToDb((float)value));
		UpdateVolumeLabel((float)value);
	}

	private void UpdateVolumeLabel(float linearValue)
	{
		if (VolumeLabel != null)
		{
			int percent = Mathf.RoundToInt(linearValue * 100);
			VolumeLabel.Text = $"{percent}%";
		}
	}

private void TogglePause()
	{
		_isPaused = !_isPaused;
		GetTree().Paused = _isPaused; 
		Visible = _isPaused;

		// USUNIĘTO: MainSceneMusic.SetMusicPaused(_isPaused);

		if (_isPaused)
		{
			if (MainMenuContainer != null) MainMenuContainer.Visible = true;
			else GD.PrintErr("CRITICAL: MainMenuContainer nie znaleziony!");

			if (SettingsContainer != null) SettingsContainer.Visible = false;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel")) 
		{
			if (Visible && SettingsContainer != null && SettingsContainer.Visible)
			{
				CloseSettings();
			}
			else
			{
				TogglePause();
			}
		}
	}

	private void OpenSettings()
	{
		if (MainMenuContainer != null) MainMenuContainer.Visible = false;
		if (SettingsContainer != null) SettingsContainer.Visible = true;
	}

	private void CloseSettings()
	{
		if (SettingsContainer != null) SettingsContainer.Visible = false;
		if (MainMenuContainer != null) MainMenuContainer.Visible = true;
	}

	private void ToggleGender()
	{
		if (MainGameManager.Instance == null) return;

		bool newState = !MainGameManager.Instance.IsMale;
		MainGameManager.Instance.SetGender(newState);

		if (GednerLabel != null)
		{
			GednerLabel.Text = newState ? "PŁEĆ: MĘŻCZYZNA" : "PŁEĆ: KOBIETA";
		}
	}
}
