using Godot;
using System;
using System.Collections.Generic;

public partial class NotificationUI : CanvasLayer
{
	public static NotificationUI Instance { get; private set; }

	[Export] public Label TypeLabel;
	[Export] public Label TitleLabel;
	[Export] public Control NotificationPanel; 
	[Export] public AnimationPlayer AnimPlayer;
	[Export] public AudioStreamPlayer AudioPlayer;
	
	// NOWE: Miejsce na ikonkę w scenie
	[Export] public TextureRect IconRect; 

	// NOWE: Tu wrzucisz swoje obrazki w Inspektorze
	[Export] public Texture2D IconQuestStart;
	[Export] public Texture2D IconQuestUpdate;
	[Export] public Texture2D IconQuestComplete;
	[Export] public Texture2D IconItem;
	[Export] public Texture2D IconAchievement;

	[Export] public AudioStream SoundQuestStart;
	[Export] public AudioStream SoundQuestUpdate;
	[Export] public AudioStream SoundQuestComplete;
	[Export] public AudioStream SoundAchievement;
	[Export] public AudioStream SoundItemGet;

	private Queue<NotificationData> _queue = new Queue<NotificationData>();
	private bool _isShowing = false;

	private struct NotificationData
	{
		public string Type;
		public string Title;
	}

	public override void _Ready()
	{
		Instance = this;
		if (NotificationPanel != null) NotificationPanel.Modulate = new Color(1, 1, 1, 0);
		CallDeferred(nameof(SubscribeToManager));
	}

	private void SubscribeToManager()
	{
		if (QuestManager.Instance != null)
			QuestManager.Instance.OnQuestNotification += AddNotification;
	}

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

		if (TypeLabel != null) TypeLabel.Text = data.Type;
		if (TitleLabel != null) TitleLabel.Text = data.Title;

		ConfigureNotificationStyle(data.Type);

		if (AnimPlayer != null)
		{
			AnimPlayer.Play("slide_in");
			await ToSignal(AnimPlayer, "animation_finished");
		}

		_isShowing = false;
		TryShowNext();
	}

	private void ConfigureNotificationStyle(string type)
	{
		Color typeColor = Colors.White;
		AudioStream soundToPlay = null;
		Texture2D iconToSet = null; // Zmienna na ikonę

		if (type.Contains("NOWE ZADANIE"))
		{
			typeColor = Colors.Gold;
			soundToPlay = SoundQuestStart;
			iconToSet = IconQuestStart;
		}
		else if (type.Contains("ZAKTUALIZOWANO"))
		{
			typeColor = Colors.DeepSkyBlue;
			soundToPlay = SoundQuestUpdate;
			iconToSet = IconQuestUpdate;
		}
		else if (type.Contains("UKOŃCZONE"))
		{
			typeColor = Colors.GreenYellow;
			soundToPlay = SoundQuestComplete;
			iconToSet = IconQuestComplete;
		}
		else if (type.Contains("OSIĄGNIĘCIE"))
		{
			typeColor = Colors.Magenta;
			soundToPlay = SoundAchievement;
			iconToSet = IconAchievement;
		}
		else if (type.Contains("PRZEDMIOT") || type.Contains("ITEM"))
		{
			typeColor = Colors.Orange;
			soundToPlay = SoundItemGet;
			iconToSet = IconItem;
		}

		if (TypeLabel != null) TypeLabel.Modulate = typeColor;
		
		// Ustaw ikonę (jeśli mamy ikonę i TextureRect)
		if (IconRect != null)
		{
			IconRect.Texture = iconToSet;
			IconRect.Visible = (iconToSet != null); // Ukryj jeśli brak ikony
		}

		if (AudioPlayer != null && soundToPlay != null)
		{
			AudioPlayer.Stream = soundToPlay;
			AudioPlayer.Play();
		}
	}
}
