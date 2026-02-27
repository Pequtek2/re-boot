using Godot;
using System;

public partial class MainMenu : Control
{
	private Button _startButton;
	private Button _exitButton;
	private FaderLayer _fader;
	[Export] public string TargetMachineID = "machine_4";

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
		MainGameManager.Instance.SetMachineFixed(TargetMachineID);
		QuestManager.Instance.ProgressQuest("main_quest_4", 1);
		QuestManager.Instance.ProgressQuest("story_main", 1);
		TagManager.Instance.AddTag("machine_4_fixed");
				
		GD.Print($"Minigra wygrana. Maszyna: 4, Quest zaktualizowany.");
		GD.Print($"SUKCES! Maszyna 4 została naprawiona.");
		await _fader.FadeOut();
		GetTree().ChangeSceneToFile("res://Scenes/Main/FactoryHub.tscn");
	}
}
