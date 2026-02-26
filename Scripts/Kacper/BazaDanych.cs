using Godot;
using System;
using System.Threading.Tasks;

public partial class BazaDanych : Node2D 
{
	[Export] public ColorRect GlitchOverlay;

	private SoundManager _sound;
	private AnimationPlayer _anim;

	public override async void _Ready()
	{
		_sound = GetTree().Root.FindChild("SoundManager", true, false) as SoundManager;
		_anim = GetNode<AnimationPlayer>("AnimationPlayer");

		if (GlitchOverlay?.Material is ShaderMaterial mat)
			mat.SetShaderParameter("shake_rate", 0.0f);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		// Dźwięk startowy komputera
		_sound?.PlayByName("Odpalenie kompa ");

		if (_anim != null)
		{
			await Task.Delay(500);
			_anim.Play("loading");
		}
	}

	public void PlayBlip()
	{
		_sound?.PlayByName("loadingscreenblip");
	}

	public void PlayRandomGlitch()
	{
		string randomSound = GD.Randi() % 2 == 0 ? "glitch" : "glitch2";
		_sound?.PlayByName(randomSound);
		
		if (GlitchOverlay?.Material is ShaderMaterial mat)
		{
			mat.SetShaderParameter("shake_rate", 0.7f);
			GetTree().CreateTimer(0.2f).Timeout += () => mat.SetShaderParameter("shake_rate", 0.0f);
		}
	}

	// Ta metoda odpala się po kliknięciu przycisku w edytorze
	public async void _on_button_pressed()
	{
		var transitioner = GetNodeOrNull<Transitioner>("CanvasLayer/Transitioner");
		
		// Odtwarzamy dźwięk kliknięcia myszy
		_sound?.PlayByName("mouseclick");

		if (transitioner != null)
		{
			// Przejście do następnej sceny
			await transitioner.ChangeScene("res://Scenes/Kacper/MenuGlowne.tscn", true);
		}
		else
		{
			GetTree().ChangeSceneToFile("res://Scenes/Kacper/MenuGlowne.tscn");
		}
	}
}
