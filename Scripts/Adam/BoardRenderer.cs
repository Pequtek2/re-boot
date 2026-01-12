using Godot;
using System;

public partial class BoardRenderer : Node2D
{
	public int CurrentLevel = 0;

	private Color _gridColor = Color.FromHtml("#1a232e");
	private Color _boardColor = Color.FromHtml("#112211");
	private Color _boardBorder = Color.FromHtml("#224422");
	private Color _wireColor = Color.FromHtml("#334455"); 
	private Font _defaultFont;

	public override void _Ready()
	{
		_defaultFont = ThemeDB.FallbackFont;
	}
	
	public override void _Draw()
	{
		Vector2 viewportSize = GetViewportRect().Size;
		Vector2 localStart = -Position; 
		Vector2 localEnd = localStart + viewportSize;

		DrawRect(new Rect2(localStart, viewportSize), Color.FromHtml("#0d1218"), true);

		float startX = Mathf.Floor(localStart.X / 40) * 40;
		float startY = Mathf.Floor(localStart.Y / 40) * 40;

		for (float x = startX; x < localEnd.X; x += 40)
			DrawLine(new Vector2(x, localStart.Y), new Vector2(x, localEnd.Y), _gridColor);
		for (float y = startY; y < localEnd.Y; y += 40)
			DrawLine(new Vector2(localStart.X, y), new Vector2(localEnd.X, y), _gridColor);

		Rect2 pcbRect = new Rect2(100, 80, 600, 350);
		DrawRect(pcbRect, _boardColor, true);
		DrawRect(pcbRect, _boardBorder, false, 4.0f);

		DrawLevelLayout(CurrentLevel);
	}

	private void DrawLevelLayout(int level)
	{
		if (level == 0) // Poziom 1: Latarka
		{
			DrawOrthogonalWire(new Vector2(200, 220), new Vector2(370, 150), true);
			DrawOrthogonalWire(new Vector2(430, 150), new Vector2(600, 220), false);
			DrawFixedComponent(600, 250, "bulb");
			DrawWireSegment(new Vector2(600, 280), new Vector2(600, 350));
			DrawWireSegment(new Vector2(600, 350), new Vector2(200, 350));
			DrawWireSegment(new Vector2(200, 350), new Vector2(200, 280));
		}
		else if (level == 1) // Poziom 2: Dioda
		{
			DrawFixedComponent(150, 300, "battery");
			DrawWireSegment(new Vector2(170, 300), new Vector2(270, 300));
			DrawWireSegment(new Vector2(330, 300), new Vector2(470, 300));
			DrawOrthogonalWire(new Vector2(530, 300), new Vector2(650, 380));
			DrawWireSegment(new Vector2(650, 380), new Vector2(120, 380));
			DrawWireSegment(new Vector2(120, 380), new Vector2(120, 300));
			DrawWireSegment(new Vector2(120, 300), new Vector2(130, 300));
		}
		else if (level == 2) // Poziom 3: Filtr RC
		{
			DrawFixedComponent(150, 200, "battery");
			DrawWireSegment(new Vector2(170, 200), new Vector2(270, 200));
			DrawWireSegment(new Vector2(330, 200), new Vector2(450, 200));
			DrawCircle(new Vector2(450, 200), 5, _wireColor);
			DrawWireSegment(new Vector2(450, 200), new Vector2(450, 270));
			DrawWireSegment(new Vector2(450, 330), new Vector2(450, 380));
			DrawOrthogonalWire(new Vector2(450, 200), new Vector2(600, 240)); 
			DrawFixedComponent(600, 240, "led_empty"); 
			DrawOrthogonalWire(new Vector2(600, 260), new Vector2(130, 380), true);
			DrawCircle(new Vector2(450, 380), 5, _wireColor);
			DrawWireSegment(new Vector2(130, 380), new Vector2(130, 200));
		}
		else if (level == 3) // Poziom 4: Silnik
		{
			DrawFixedComponent(150, 300, "battery");
			DrawWireSegment(new Vector2(170, 300), new Vector2(250, 300));
			DrawWireSegment(new Vector2(310, 300), new Vector2(400, 300));
			DrawWireSegment(new Vector2(460, 300), new Vector2(575, 300));
			DrawFixedComponent(600, 300, "motor");
			DrawOrthogonalWire(new Vector2(625, 300), new Vector2(680, 420));
			DrawWireSegment(new Vector2(680, 420), new Vector2(120, 420));
			DrawWireSegment(new Vector2(120, 420), new Vector2(120, 300));
			DrawWireSegment(new Vector2(120, 300), new Vector2(130, 300));
		}
	}

	private void DrawWireSegment(Vector2 start, Vector2 end)
	{
		DrawLine(start, end, _wireColor, 6.0f);
		DrawCircle(start, 3, _wireColor);
		DrawCircle(end, 3, _wireColor);
	}

	private void DrawOrthogonalWire(Vector2 start, Vector2 end, bool verticalFirst = false)
	{
		Vector2 elbow;
		if (verticalFirst)
			elbow = new Vector2(start.X, end.Y);
		else
			elbow = new Vector2(end.X, start.Y);

		DrawWireSegment(start, elbow);
		DrawWireSegment(elbow, end);
		DrawCircle(elbow, 3, _wireColor);
	}

	private void DrawFixedComponent(float x, float y, string type)
	{
		if (type == "bulb")
		{
			DrawCircle(new Vector2(x, y), 20, new Color(0.2f, 0.2f, 0.2f));
			DrawArc(new Vector2(x, y), 20, 0, 360, 32, Colors.Gray, 2.0f);
		}
		else if (type == "battery")
		{
			DrawRect(new Rect2(x - 20, y - 30, 40, 60), new Color(0.2f, 0.2f, 0.2f), true);
			DrawString(_defaultFont, new Vector2(x - 5, y + 5), "+", HorizontalAlignment.Center, -1, 24, Colors.Magenta);
		}
		else if (type == "led_empty")
		{
			 DrawCircle(new Vector2(x, y), 10, new Color(0.1f, 0.1f, 0.1f));
			 DrawCircle(new Vector2(x, y), 3, Colors.Red);
		}
		else if (type == "motor")
		{
			DrawCircle(new Vector2(x, y), 25, new Color(0.3f, 0.3f, 0.4f)); 
			DrawRect(new Rect2(x - 5, y - 5, 10, 10), Colors.Gray, true); 
			DrawString(_defaultFont, new Vector2(x - 5, y - 15), "M", HorizontalAlignment.Center, -1, 16, Colors.White);
		}
	}
}
