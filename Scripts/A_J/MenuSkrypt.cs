using Godot;

public partial class MenuSkrypt : Control
{
	public override void _Ready()
	{
		// Sprawdzamy czy przyciski są tam gdzie powinny
		Button btnStart = GetNode<Button>("VBoxContainer/Button");
		Button btnExit = GetNode<Button>("VBoxContainer/Button2");

		btnStart.Pressed += () => {
			GD.Print("Przełączam na grę...");
			GetTree().ChangeSceneToFile("res://Scenes/A_J/MiniGameA_J.tscn");
		};

		btnExit.Pressed += () => GetTree().Quit();
	}
}
