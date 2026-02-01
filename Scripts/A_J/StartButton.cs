using Godot;

public partial class StartButton : Button
{
	public override void _Pressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/A_J/MiniGameA_J.tscn");
	}
}
