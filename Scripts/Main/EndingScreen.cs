using Godot;
using System;

public partial class EndingScreen : CanvasLayer
{
	public static EndingScreen Instance { get; private set; }

	[ExportGroup("Ustawienia Końcowe")]
	[Export] public string MainMenuPath = "res://Scenes/Menu/Menu.tscn"; 
	[Export] public float ScrollSpeed = 60.0f; 
	[Export] public float WaitTime = 5.0f;     

	[ExportGroup("Dźwięk i UI")]
	[Export] public AudioStreamPlayer CongratsSound;
	[Export] public AudioStreamPlayer CreditsMusic;
	[Export] public Button RestartButton; 

	private Control _background;
	private TextureRect _handshakeImage;
	private Label _congratsText;
	private Label _creditsText;

	private bool _isScrolling = false;

	public override void _Ready()
	{
		Instance = this;

		_background = GetNodeOrNull<Control>("Background"); 
		_handshakeImage = GetNodeOrNull<TextureRect>("HandshakeImage");
		_congratsText = GetNodeOrNull<Label>("CongratsText");
		_creditsText = GetNodeOrNull<Label>("CreditsText");

		Visible = false;

		if (RestartButton != null)
		{
			RestartButton.Visible = false;
			RestartButton.Pressed += OnRestartButtonPressed;
		}
	}

	public void StartEndingSequence()
	{
		Visible = true;
		if (_background != null) _background.Visible = true;
		if (_handshakeImage != null) _handshakeImage.Visible = true;
		if (_congratsText != null) _congratsText.Visible = true;
		if (_creditsText != null) _creditsText.Visible = false;
		if (RestartButton != null) RestartButton.Visible = false;

		// ==================================================
		// NOWE: Zatrzymujemy główną muzykę w tle
		// ==================================================
		MainSceneMusic.PauseBackgroundMusic();

		if (CongratsSound != null) CongratsSound.Play();

		GetTree().CreateTimer(WaitTime).Timeout += StartCredits;
	}

	private void StartCredits()
	{
		if (_handshakeImage != null) _handshakeImage.Visible = false;
		if (_congratsText != null) _congratsText.Visible = false;

		if (CreditsMusic != null) CreditsMusic.Play();

		if (_creditsText != null)
		{
			_creditsText.Visible = true;
			float screenHeight = GetViewport().GetVisibleRect().Size.Y;
			_creditsText.Position = new Vector2(_creditsText.Position.X, screenHeight);
			_isScrolling = true;
		}
	}

	public override void _Process(double delta)
	{
		if (_isScrolling && _creditsText != null)
		{
			_creditsText.Position = new Vector2(_creditsText.Position.X, _creditsText.Position.Y - ScrollSpeed * (float)delta);

			if (_creditsText.Position.Y + _creditsText.Size.Y < 0)
			{
				_isScrolling = false;
				ShowRestartButton();
			}
		}
	}

	private void ShowRestartButton()
	{
		if (RestartButton != null)
		{
			RestartButton.Visible = true;
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	private void OnRestartButtonPressed()
	{
		if (!string.IsNullOrEmpty(MainMenuPath))
		{
			GetTree().ChangeSceneToFile(MainMenuPath);
		}
	}
}
