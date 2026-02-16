using Godot;

public partial class ExitButton : Button
{
	public override void _Pressed()
	{
		// Ta linijka po prostu zamyka całą aplikację
		GetTree().Quit();
	}
}
