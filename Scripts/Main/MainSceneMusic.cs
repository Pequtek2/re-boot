using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MainSceneMusic : Node
{
	[ExportGroup("Odtwarzacz Muzyki")]
	[Export] public Godot.Collections.Array<AudioStream> BackgroundMusic;

	private static AudioStreamPlayer _globalMusicPlayer;
	private static List<AudioStream> _playlist = new List<AudioStream>();
	private static int _currentTrackIndex = 0;
	private static Random _rng = new Random();
	private static bool _isInitialized = false;

	public override void _Ready()
	{
		// Opóźniamy całą inicjalizację, aby mieć 100% pewności, 
		// że węzeł może bezpiecznie dołączyć do drzewa gry.
		CallDeferred(MethodName.InitMusicPlayer);
	}

	private void InitMusicPlayer()
	{
		// 1. Tworzymy odtwarzacz
		if (_globalMusicPlayer == null)
		{
			_globalMusicPlayer = new AudioStreamPlayer();
			
			// WAŻNE: Przypisujemy do kanału "Music", żeby suwak w pauzie na niego działał!
			_globalMusicPlayer.Bus = "Music"; 
			_globalMusicPlayer.ProcessMode = ProcessModeEnum.Always;
			
			// Bezpieczne dodanie do Roota
			GetTree().Root.AddChild(_globalMusicPlayer);
			
			_globalMusicPlayer.Finished += PlayNextTrack;
		}

		// 2. Ładujemy i tasujemy piosenki
		if (!_isInitialized && BackgroundMusic != null && BackgroundMusic.Count > 0)
		{
			_playlist = new List<AudioStream>(BackgroundMusic);
			_playlist = _playlist.OrderBy(x => _rng.Next()).ToList();
			_isInitialized = true;
			
			PlayNextTrack();
		}

		// 3. Wznowienie po powrocie z minigry
		if (_globalMusicPlayer != null && _globalMusicPlayer.StreamPaused)
		{
			_globalMusicPlayer.StreamPaused = false;
		}
	}

	public override void _ExitTree()
	{
		if (_globalMusicPlayer != null && _globalMusicPlayer.Playing)
		{
			_globalMusicPlayer.StreamPaused = true;
		}
	}

	private static void PlayNextTrack()
	{
		if (_playlist.Count == 0 || _globalMusicPlayer == null) return;

		if (_currentTrackIndex >= _playlist.Count)
		{
			_currentTrackIndex = 0;
			_playlist = _playlist.OrderBy(x => _rng.Next()).ToList();
		}

		_globalMusicPlayer.Stream = _playlist[_currentTrackIndex];
		_globalMusicPlayer.Play();
		_currentTrackIndex++;
	}
}
