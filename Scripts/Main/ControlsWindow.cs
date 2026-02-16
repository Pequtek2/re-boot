using Godot;
using System;

public partial class ControlsWindow : Control
{
	// Singleton - pozwala odwołać się do tego okna z DialogueManagera
	public static ControlsWindow Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
		Visible = false; // Na starcie zawsze ukryte (czeka na komendę z dialogu)
	}

	public override void _Input(InputEvent @event)
	{
		// Obsługa klawisza "K"
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.K)
			{
				ToggleWindow();
			}
		}
	}

	// Podepnij tę funkcję pod guzik "X" (Zamknij) oraz guzik w HUDzie
	public void ToggleWindow()
	{
		Visible = !Visible;
	}

	// Tę funkcję wywoła komenda "show_controls" z Twojego pliku JSON
	public void ForceShow()
	{
		Visible = true;
	}
}
