using Godot;
using System;

public partial class MainMenu : Control
{
	private Button _startButton;
	private Button _exitButton;
	private FaderLayer _fader;

	public override void _Ready()
	{
		_startButton = GetNode<Button>("MenuBox/StartButton");
		_exitButton = GetNode<Button>("MenuBox/ExitButton");

		_startButton.Pressed += OnStartPressed;
		_exitButton.Pressed += OnExitPressed;
		
		_exitButton.Disabled = !GameState.QuizFinished;
		
		_fader = GetNodeOrNull<FaderLayer>("FaderLayer");
	}

	private async void OnStartPressed()
	{
		await _fader.FadeOut();
		GetTree().ChangeSceneToFile("res://Scenes/fasolaaa/AboutGame.tscn");
	}

	private async void OnExitPressed()
	{
		await _fader.FadeOut();
		GetTree().ChangeSceneToFile("res://Scenes/Main/FactoryHub.tscn");
	}
}
