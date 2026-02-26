using Godot;
using System;

public partial class SoundManager : Node
{
	private AudioStreamPlayer _loopPlayer; // Dla efektów zapętlonych (np. pisanie)
	private AudioStreamPlayer _musicPlayer; // Dla muzyki tła (BGM)

	public override void _Ready()
	{
		// Konfiguracja odtwarzacza efektów zapętlonych
		_loopPlayer = new AudioStreamPlayer();
		_loopPlayer.Bus = "SFX"; // Ustawienie szyny na SFX
		AddChild(_loopPlayer);

		// Konfiguracja odtwarzacza muzyki
		_musicPlayer = new AudioStreamPlayer();
		_musicPlayer.Bus = "Music"; // Ustawienie szyny na Music
		AddChild(_musicPlayer);
	}

	// --- GLOBALNE STEROWANIE GŁOŚNOŚCIĄ ---
	// Pozwala ściszać/zgłaśniać konkretną szynę (np. "Music", "SFX" lub "Master")
	// value powinno być w zakresie od 0.0 (cisza) do 1.0 (max)
	public void SetVolume(string busName, float value)
	{
		int busIndex = AudioServer.GetBusIndex(busName);
		if (busIndex != -1)
		{
			// Konwersja liniowej głośności na decybele
			float db = Mathf.LinearToDb(Mathf.Max(value, 0.0001f));
			AudioServer.SetBusVolumeDb(busIndex, db);
			AudioServer.SetBusMute(busIndex, value <= 0.0001f);
		}
	}

	// --- OBSŁUGA MUZYKI (BGM) ---
	public void PlayBGM(string fileName)
	{
		string path = "res://Sounds/Kacper/" + fileName;

		if (!ResourceLoader.Exists(path))
		{
			GD.PrintErr("BŁĄD: Nie znaleziono pliku muzyki: " + path);
			return;
		}

		AudioStream musicStream = GD.Load<AudioStream>(path);

		// Jeśli utwór już gra, nie restartujemy go
		if (_musicPlayer.Stream == musicStream && _musicPlayer.Playing) 
			return;

		_musicPlayer.Stream = musicStream;
		_musicPlayer.Play();
	}

	public void StopBGM()
	{
		if (_musicPlayer.Playing)
			_musicPlayer.Stop();
	}

	// --- OBSŁUGA EFEKTÓW (SFX) ---
	public void PlayByName(string soundName)
	{
		string folder = "res://Sounds/Kacper/";
		AudioStream sfx = null;

		// Sprawdzanie dostępnych rozszerzeń
		if (ResourceLoader.Exists(folder + soundName + ".wav")) 
			sfx = GD.Load<AudioStream>(folder + soundName + ".wav");
		else if (ResourceLoader.Exists(folder + soundName + ".ogg")) 
			sfx = GD.Load<AudioStream>(folder + soundName + ".ogg");

		if (sfx != null)
		{
			AudioStreamPlayer tempPlayer = new AudioStreamPlayer();
			tempPlayer.Stream = sfx;
			tempPlayer.Bus = "SFX"; // Przypisanie do szyny efektów
			AddChild(tempPlayer);
			tempPlayer.Play();
			// Automatyczne usuwanie odtwarzacza po zakończeniu dźwięku
			tempPlayer.Finished += () => tempPlayer.QueueFree();
		}
	}

	public void PlayPopup() => PlayByName("popupsound");

	public void StartTypingSound()
	{
		string path = "res://Sounds/Kacper/pisanie.ogg";
		if (!ResourceLoader.Exists(path)) return;

		var sfx = GD.Load<AudioStream>(path);
		if (sfx != null && !_loopPlayer.Playing)
		{
			_loopPlayer.Stream = sfx;
			_loopPlayer.Play();
		}
	}

	public void StopTypingSound() => _loopPlayer.Stop();
}
