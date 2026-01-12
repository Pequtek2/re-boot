using Godot;
using System;

public partial class Slot : Area2D
{
	[Export] public int SlotId = 0;
	[Export] public string RequiredType = "wire";
	
	public DraggableItem OccupyingItem = null;

	public override void _Draw()
	{
		// Rysujemy ramkę slotu (przerywana linia symulowana przez DrawRect z false)
		Rect2 rect = new Rect2(-30, -30, 60, 60);
		
		// Półprzezroczyste tło
		DrawRect(rect, new Color(0, 0, 0, 0.5f), true);
		
		// Obrys w kolorze "technicznym"
		DrawRect(rect, new Color(0.3f, 0.4f, 0.5f), false, 2.0f);
	}
	
	public override void _Ready()
	{
		// Jeśli slot ma Label, ustawmy mu kolor neonowy
		var label = GetNodeOrNull<Label>("Label");
		if (label != null)
		{
			label.Modulate = new Color(0, 1, 0.8f); // Neon Cyan
			label.Position = new Vector2(-50, -55); // Nad slotem
			label.HorizontalAlignment = HorizontalAlignment.Center;
		}
	}

	public void SetLabel(string text)
	{
		var label = GetNodeOrNull<Label>("Label");
		if (label != null) label.Text = text;
	}

	public bool IsOccupied() => OccupyingItem != null;
}
