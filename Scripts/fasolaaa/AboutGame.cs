using Godot;
using System;

public partial class AboutGame : Control
{
	private Button _dalejButton;
	private VideoStreamPlayer _previewVideo;
	private ColorRect _fadeRect;
	private bool _isTransitioning = false;
	private FaderLayer _fader;


	public override void _Ready()
	{
		_dalejButton = GetNode<Button>("dalejButton");
		_dalejButton.Pressed += OnStartPressed;
		
		_previewVideo = GetNode<VideoStreamPlayer>("PreviewVideo");
		_previewVideo.Finished += () => _previewVideo.Play(); // zapętlenie
		_previewVideo.Play();
		
		_fader = GetNodeOrNull<FaderLayer>("FaderLayer");
	}
	
	private async void OnStartPressed()
	{
		await _fader.FadeOut();
		GetTree().ChangeSceneToFile("res://Scenes/fasolaaa/okej.tscn");
	}
}
