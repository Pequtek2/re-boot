using Godot;
using System;
using System.Collections.Generic;

public partial class NotificationUI : CanvasLayer
{
	// Singleton - aby można było wywołać powiadomienie z każdego miejsca w grze
	public static NotificationUI Instance { get; private set; }

	[Export] public Label TypeLabel;
	[Export] public Label TitleLabel;
	[Export] public Control NotificationPanel; // PanelContainer lub ColorRect
	[Export] public AnimationPlayer AnimPlayer;
	[Export] public AudioStreamPlayer AudioPlayer;

	[Export] public AudioStream SoundQuestStart;    // Np. start.wav
	[Export] public AudioStream SoundQuestUpdate;   // Np. click.wav
	[Export] public AudioStream SoundQuestComplete; // Np. sucess.ogg
	[Export] public AudioStream SoundAchievement;   // Np. win.mp3
	[Export] public AudioStream SoundItemGet;       // Np. drop.ogg

	// Kolejka wiadomości
	private Queue<NotificationData> _queue = new Queue<NotificationData>();
	private bool _isShowing = false;

	private struct NotificationData
	{
		public string Type;  // np. "NOWE ZADANIE", "OSIĄGNIĘCIE"
		public string Title; // np. "Napraw robota", "Mistrz napraw"
	}

	public override void _Ready()
	{
		Instance = this;

		// Ukryj panel na starcie (przezroczystość)
		if (NotificationPanel != null) 
			NotificationPanel.Modulate = new Color(1, 1, 1, 0);

		// Subskrypcja QuestManagera (automatyczna)
		CallDeferred(nameof(SubscribeToManager));
	}

	private void SubscribeToManager()
	{
		if (QuestManager.Instance != null)
		{
			QuestManager.Instance.OnQuestNotification += AddNotification;
		}
	}

	// Tę metodę możesz wywołać z dowolnego skryptu!
	// Np. NotificationUI.Instance.AddNotification("PRZEDMIOT", "Klucz do serwerowni");
	public void AddNotification(string type, string title)
	{
		_queue.Enqueue(new NotificationData { Type = type, Title = title });
		TryShowNext();
	}

	private async void TryShowNext()
	{
		if (_isShowing || _queue.Count == 0) return;

		_isShowing = true;
		var data = _queue.Dequeue();

		// 1. Ustaw teksty
		if (TypeLabel != null) TypeLabel.Text = data.Type;
		if (TitleLabel != null) TitleLabel.Text = data.Title;

		// 2. Ustaw kolory i dźwięki w zależności od typu
		ConfigureNotificationStyle(data.Type);

		// 3. Odegraj animację
		if (AnimPlayer != null)
		{
			AnimPlayer.Play("slide_in");
			await ToSignal(AnimPlayer, "animation_finished");
		}

		_isShowing = false;
		TryShowNext(); // Sprawdź czy coś jeszcze czeka w kolejce
	}

	private void ConfigureNotificationStyle(string type)
	{
		// Domyślny kolor (biały/cyjan)
		Color typeColor = Colors.Cyan;
		AudioStream soundToPlay = null;

		if (type.Contains("NOWE ZADANIE"))
		{
			typeColor = Colors.Gold;
			soundToPlay = SoundQuestStart;
		}
		else if (type.Contains("ZAKTUALIZOWANO"))
		{
			typeColor = Colors.DeepSkyBlue;
			soundToPlay = SoundQuestUpdate;
		}
		else if (type.Contains("UKOŃCZONE"))
		{
			typeColor = Colors.GreenYellow;
			soundToPlay = SoundQuestComplete;
		}
		else if (type.Contains("OSIĄGNIĘCIE"))
		{
			typeColor = Colors.Magenta; // Fioletowy dla osiągnięć
			soundToPlay = SoundAchievement;
		}
		else if (type.Contains("PRZEDMIOT") || type.Contains("ITEM"))
		{
			typeColor = Colors.Orange; // Pomarańczowy dla przedmiotów
			soundToPlay = SoundItemGet;
		}

		// Zastosuj kolor do etykiety typu
		if (TypeLabel != null) TypeLabel.Modulate = typeColor;

		// Odtwórz dźwięk
		if (AudioPlayer != null && soundToPlay != null)
		{
			AudioPlayer.Stream = soundToPlay;
			AudioPlayer.Play();
		}
	}
}
