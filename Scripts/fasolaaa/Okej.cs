using Godot;
using System;
using System.Collections.Generic;

public partial class Okej : Control
{
	private class Question
	{
		public string Text;
		public string[] Answers; // zawsze 4
		public int CorrectIndex; // 0..3

		public Question(string text, string[] answers, int correctIndex)
		{
			Text = text;
			Answers = answers;
			CorrectIndex = correctIndex;
		}
	}

	private Label _questionLabel;
	private Label _counterLabel;
	private Label _feedbackLabel;
	private AudioStreamPlayer _musicPlayer;
	private AudioStreamPlayer _clickPlayer;
	private Control _answersBox;
	private Vector2 _questionBasePos;
	private Vector2 _answersBasePos;
	private Vector2 _feedbackBasePos;
	private Panel _settingsPanel;
	private Button _settingsButton;
	private HSlider _musicSlider;
	private HSlider _sfxSlider;
	private Button[] _answerButtons;
	private FaderLayer _fader;

	private List<Question> _questions;
	private int _current = 0;

	public override void _Ready()
	{
		_musicPlayer = GetNodeOrNull<AudioStreamPlayer>("MusicPlayer");
		_clickPlayer = GetNodeOrNull<AudioStreamPlayer>("ClickPlayer");
		
		// Podpinamy referencje do elementów UI po nazwach w scenie
		_questionLabel = GetNode<Label>("QuestionLabel");
		_counterLabel = GetNode<Label>("CounterLabel");
		_feedbackLabel = GetNode<Label>("FeedbackLabel");

		_answerButtons = new Button[4];
		_answerButtons[0] = GetNode<Button>("AnswersBox/AnswerButton1");
		_answerButtons[1] = GetNode<Button>("AnswersBox/AnswerButton2");
		_answerButtons[2] = GetNode<Button>("AnswersBox2/AnswerButton3");
		_answerButtons[3] = GetNode<Button>("AnswersBox2/AnswerButton4");
		
		_answersBox = GetNode<Control>("AnswersBox");

		_questionBasePos = _questionLabel.Position;
		_answersBasePos = _answersBox.Position;
		_feedbackBasePos = _feedbackLabel.Position;

		// Podpinamy kliknięcia przycisków do jednej funkcji (z numerem przycisku)
		for (int i = 0; i < 4; i++)
		{
			int capturedIndex = i; // ważne: kopia wartości i
			_answerButtons[i].Pressed += () => OnAnswerPressed(capturedIndex);
		}

		BuildQuestions();
		ShowQuestion();
		
		_settingsPanel = GetNodeOrNull<Panel>("SettingsPanel");
		_settingsButton = GetNodeOrNull<Button>("SettingsButton");
		_musicSlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsBox/MusicSlider");
		_sfxSlider = GetNodeOrNull<HSlider>("SettingsPanel/SettingsBox/SfxSlider");

		// przycisk otwierania
		_settingsButton.Pressed += ToggleSettings;

		// suwaki
		_musicSlider.ValueChanged += OnMusicVolumeChanged;
		_sfxSlider.ValueChanged += OnSfxVolumeChanged;
		
		_musicPlayer.Finished += () => _musicPlayer.Play();
		
		_fader = GetNodeOrNull<FaderLayer>("FaderLayer");
	}

	private void BuildQuestions()
	{
		_questions = new List<Question>
		{
			new Question(
				"1) What does 'int' represent in C++?",
				new[] { "A floating-point number", "An integer number", "A string", "A boolean" },
				1),

			new Question(
				"2) Which symbol is used to end a\nstatement in C++?",
				new[] { ":", ";", ".", "!" },
				1),

			new Question(
				"3) What is the correct way to\noutput text to the console in C++?",
				new[] { "print(\"Hello\")", "Console.WriteLine(\"Hello\")", "cout << \"Hello\";", "echo \"Hello\"" },
				2),

			new Question(
				"4) Which of these creates a\nvariable named x with value 5?",
				new[] { "int x = 5;", "x := 5", "var x == 5", "int = x 5" },
				0),

			new Question(
				"5) What does '==' mean in C++?",
				new[] { "Assignment", "Equality comparison", "Not equal", "Greater than" },
				1),

			new Question(
				"6) Which header is commonly used\nfor input/output with cout/cin?",
				new[] { "<stdio.h>", "<iostream>", "<vector>", "<string>" },
				1),

			new Question(
				"7) What is a 'for' loop used for?",
				new[] { "Storing text", "Repeating a block of code", "Defining a class", "Stopping the program" },
				1),

			new Question(
				"8) What does 'std' refer to in C++\n(like std::cout)?",
				new[] { "A graphics library", "The standard namespace", "A type of variable", "A compiler setting" },
				1),

			new Question(
				"9) Which type is best for\ntrue/false values?",
				new[] { "int", "float", "bool", "char" },
				2),

			new Question(
				"10) What does 'return 0;' usually\nmean at the end of main()?",
				new[] { "The program crashed", "The program ended successfully", "Repeat the program", "Print zero" },
				1),
		};
	}

	private void ShowQuestion()
	{
		_feedbackLabel.Text = "";

		var q = _questions[_current];
		_questionLabel.Text = q.Text;
		_counterLabel.Text = $"{_current + 1}/{_questions.Count}";

		for (int i = 0; i < 4; i++)
			_answerButtons[i].Text = q.Answers[i];
			AnimateQuestionIn();
	}

	private async void OnAnswerPressed(int index)
	{
		if (_clickPlayer != null && _clickPlayer.Stream != null)
	{
		_clickPlayer.Stop();
		_clickPlayer.Play();
	}
		var q = _questions[_current];
		
		if (index == q.CorrectIndex)
		{
			AnimateCorrect();

			// Zablokuj przyciski na moment, żeby nie dało się spamować
			foreach (var b in _answerButtons)
			b.Disabled = true;

			// Poczekaj aż animacja "Correct!" będzie widoczna
			await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);

			foreach (var b in _answerButtons)
			b.Disabled = false;
			
			_current++;
			
			if (_current >= _questions.Count)
			{
				GameState.QuizFinished = true;
				// Koniec quizu
				_questionLabel.Text = "Done! You answered all\nquestions correctly.";
				_counterLabel.Text = $"{_questions.Count}/{_questions.Count}";
				_feedbackLabel.Text = "";

				foreach (var b in _answerButtons)
					b.Disabled = true;
					
				await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
				await _fader.FadeOut();

				GetTree().ChangeSceneToFile("res://Scenes/fasolaaa/MainMenu.tscn");

				return;
			}

			ShowQuestion();
		}
		else
		{
			_feedbackLabel.Text = "Wrong answer. Try again!";
			AnimateWrong();
		}
	}
	
	private void AnimateQuestionIn()
	{
		// start: trochę wyżej i przezroczyste
		_questionLabel.Position = _questionBasePos + new Vector2(0, -25);
		_questionLabel.Modulate = new Color(1, 1, 1, 0);

		_answersBox.Position = _answersBasePos + new Vector2(0, 20);
		_answersBox.Modulate = new Color(1, 1, 1, 0);

		var t = CreateTween();
		t.SetParallel(true);

		t.TweenProperty(_questionLabel, "position", _questionBasePos, 0.25f)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);

		t.TweenProperty(_questionLabel, "modulate", new Color(1, 1, 1, 1), 0.25f);

		t.TweenProperty(_answersBox, "position", _answersBasePos, 0.25f)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);

		t.TweenProperty(_answersBox, "modulate", new Color(1, 1, 1, 1), 0.25f);
	}
	
	private void AnimateWrong()
	{
		_feedbackLabel.Position = _feedbackBasePos;
		_feedbackLabel.Modulate = new Color(1, 0.3f, 0.3f, 1);

		var t = CreateTween();
		t.TweenProperty(_feedbackLabel, "position", _feedbackBasePos + new Vector2(-10, 0), 0.05f);
		t.TweenProperty(_feedbackLabel, "position", _feedbackBasePos + new Vector2(10, 0), 0.05f);
		t.TweenProperty(_feedbackLabel, "position", _feedbackBasePos + new Vector2(-6, 0), 0.05f);
		t.TweenProperty(_feedbackLabel, "position", _feedbackBasePos + new Vector2(6, 0), 0.05f);
		t.TweenProperty(_feedbackLabel, "position", _feedbackBasePos, 0.05f);
	}
	
	private void AnimateButtonPress(int index)
	{
		var b = _answerButtons[index];
		b.Scale = Vector2.One;

		var t = CreateTween();
		t.TweenProperty(b, "scale", new Vector2(1.05f, 1.05f), 0.06f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		t.TweenProperty(b, "scale", Vector2.One, 0.08f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
	}
	
	private void AnimateCorrect()
	{
		_feedbackLabel.Text = "Correct!";
		_feedbackLabel.Modulate = new Color(0.4f, 1f, 0.4f, 0);
		_feedbackLabel.Position = _feedbackBasePos;

		var t = CreateTween();
		t.TweenProperty(_feedbackLabel, "modulate", new Color(0.4f, 1f, 0.4f, 1), 0.12f);
		t.TweenProperty(_feedbackLabel, "modulate", new Color(0.4f, 1f, 0.4f, 0), 0.25f);
	}
	
	private void ToggleSettings()
	{
		_settingsPanel.Visible = !_settingsPanel.Visible;
	}
	
	private void OnMusicVolumeChanged(double value)
	{
		int busIndex = AudioServer.GetBusIndex("Music");
		AudioServer.SetBusVolumeDb(busIndex, (float)value);
	}

	private void OnSfxVolumeChanged(double value)
	{
		int busIndex = AudioServer.GetBusIndex("SFX");
		AudioServer.SetBusVolumeDb(busIndex, (float)value);
	}
}
