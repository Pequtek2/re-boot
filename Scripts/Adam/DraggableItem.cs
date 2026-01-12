using Godot;
using System;

public partial class DraggableItem : Area2D
{
	[Export] public string ItemType = "wire"; 
	[Export] public string ItemName = "Przedmiot";
	
	public static bool IsAnyDragging = false;

	private bool _dragging = false;
	private Vector2 _originalPosition;
	public Slot CurrentSlot = null;

	[Signal]
	public delegate void DragEndedEventHandler(DraggableItem item);

	private Font _defaultFont;
	private Control _tooltip; 

	// Kolory
	private readonly Color C_NEON_BLUE = Color.FromHtml("#00ffcc");
	private readonly Color C_NEON_PINK = Color.FromHtml("#ff0055");
	private readonly Color C_DARK_BASE = Color.FromHtml("#444444");
	private readonly Color C_COPPER = Color.FromHtml("#cc6666");
	private readonly Color C_ALUMINIUM = Color.FromHtml("#aaaaaa");
	private readonly Color C_GOLD = Color.FromHtml("#ffd700");

	public override void _Ready()
	{
		_originalPosition = GlobalPosition;
		InputEvent += OnInputEvent;
		_defaultFont = ThemeDB.FallbackFont;

		var label = GetNodeOrNull<Label>("NameLabel");
		if (label != null) label.Visible = false;

		CreateTooltip();
		MouseEntered += () => { if(!_dragging) _tooltip.Visible = true; };
		MouseExited += () => _tooltip.Visible = false;
	}

	private void CreateTooltip()
	{
		_tooltip = new PanelContainer();
		_tooltip.ZIndex = 100;
		_tooltip.Visible = false;
		_tooltip.MouseFilter = Control.MouseFilterEnum.Ignore; 
		AddChild(_tooltip);

		var style = new StyleBoxFlat();
		style.BgColor = new Color(0, 0.05f, 0.1f, 0.9f);
		style.BorderWidthBottom = 1; style.BorderWidthTop = 1; style.BorderWidthLeft = 1; style.BorderWidthRight = 1;
		style.BorderColor = C_NEON_BLUE;
		style.ContentMarginLeft = 5; style.ContentMarginRight = 5; style.ContentMarginTop = 2; style.ContentMarginBottom = 2;
		_tooltip.AddThemeStyleboxOverride("panel", style);

		var lbl = new Label();
		lbl.Text = ItemName.ToUpper() + "\n" + GetItemDescription(ItemType);
		lbl.LabelSettings = new LabelSettings() { FontSize = 12, FontColor = Colors.White };
		_tooltip.AddChild(lbl);
	}

	private string GetItemDescription(string type)
	{
		switch (type)
		{
			case "battery": return "Źródło napięcia stałego.";
			case "wire": return "Przewodnik elektryczny.";
			case "resistor": return "Ogranicza przepływ prądu.";
			case "led_right": return "Emituje światło (Anoda -> Katoda).";
			case "led_left": return "Emituje światło (Odwrotna polaryzacja).";
			case "cap": return "Gromadzi ładunek elektryczny.";
			case "fuse": return "Przerywa obwód przy przeciążeniu.";
			case "switch": return "Otwiera/zamyka obwód manualnie.";
			case "rope": return "Izolator organiczny.";
			case "nail": return "Przewodnik o dużej rezystancji.";
			case "wire_short": return "Krótkie połączenie (Zworka).";
			case "coil": return "Element indukcyjny.";
			default: return "Nieznany element.";
		}
	}

	public override void _Process(double delta)
	{
		if (_dragging) 
		{
			GlobalPosition = GetGlobalMousePosition();
			_tooltip.Visible = false; 
		}
		else if (_tooltip.Visible)
		{
			_tooltip.GlobalPosition = GetGlobalMousePosition() + new Vector2(15, -30);
		}
	}

	public override void _Draw()
	{
		if (CurrentSlot == null && !_dragging)
		{
			DrawRect(new Rect2(-35, -35, 70, 70), new Color(0.1f, 0.1f, 0.1f, 0.5f), true);
			DrawRect(new Rect2(-35, -35, 70, 70), C_DARK_BASE, false, 2.0f);
		}

		switch (ItemType)
		{
			case "battery": DrawBattery(); break;
			case "wire": case "wire_short": DrawWire(); break;
			case "resistor": DrawResistor(); break;
			case "led_right": DrawLed(true); break;
			case "led_left": DrawLed(false); break;
			case "rope": DrawRope(); break;
			case "cap": DrawCapacitor(); break;
			case "fuse": DrawFuse(); break;
			case "switch": DrawSwitch(); break;
			case "nail": DrawNail(); break;
			case "coil": DrawCoil(); break;
			default: DrawRect(new Rect2(-20, -20, 40, 40), C_NEON_PINK); break;
		}
		
		if (_dragging) DrawRect(new Rect2(-30, -30, 60, 60), new Color(C_NEON_BLUE, 0.3f), true);
	}

	// --- RYSOWANIE ---

	private void DrawBattery()
	{
		DrawRect(new Rect2(-20, -30, 40, 60), C_DARK_BASE, true);
		DrawRect(new Rect2(-20, -30, 40, 60), Colors.Black, false, 2.0f);
		DrawRect(new Rect2(-10, -35, 20, 5), C_ALUMINIUM, true);
		DrawString(_defaultFont, new Vector2(-8, 5), "+", HorizontalAlignment.Left, -1, 30, C_NEON_PINK);
	}

	private void DrawWire()
	{
		DrawLine(new Vector2(-30, 0), new Vector2(30, 0), C_COPPER, 8.0f);
		DrawLine(new Vector2(-30, -2), new Vector2(30, -2), new Color(1, 1, 1, 0.2f), 2.0f);
	}

	private void DrawResistor()
	{
		DrawLine(new Vector2(-35, 0), new Vector2(-25, 0), C_ALUMINIUM, 3.0f);
		DrawLine(new Vector2(25, 0), new Vector2(35, 0), C_ALUMINIUM, 3.0f);
		Rect2 body = new Rect2(-25, -12, 50, 24);
		DrawRect(body, Color.FromHtml("#eec"), true); 
		DrawRect(body, Colors.Black, false, 1.0f);
		DrawRect(new Rect2(-15, -12, 5, 24), Colors.Red, true);
		DrawRect(new Rect2(-5, -12, 5, 24), Colors.Black, true);
		DrawRect(new Rect2(5, -12, 5, 24), C_GOLD, true);
	}

	private void DrawLed(bool right)
	{
		Color ledFill = new Color(1, 0, 0, 0.4f);
		Color ledGlow = new Color(1, 0, 0, 0.8f);
		Vector2[] points = new Vector2[3];
		Vector2[] line = new Vector2[2];

		if (right) {
			points[0] = new Vector2(-15, -15); points[1] = new Vector2(-15, 15); points[2] = new Vector2(15, 0);
			line[0] = new Vector2(15, -15); line[1] = new Vector2(15, 15);
		} else {
			points[0] = new Vector2(15, -15); points[1] = new Vector2(15, 15); points[2] = new Vector2(-15, 0);
			line[0] = new Vector2(-15, -15); line[1] = new Vector2(-15, 15);
		}
		DrawColoredPolygon(points, ledFill);
		DrawPolyline(points, Colors.White, 2.0f);
		DrawLine(line[0], line[1], Colors.White, 3.0f);
		if(right) DrawCircle(new Vector2(-5, 0), 2, ledGlow); else DrawCircle(new Vector2(5, 0), 2, ledGlow);
	}

	private void DrawRope()
	{
		Color ropeColor = Color.FromHtml("#854");
		DrawRect(new Rect2(-25, -5, 50, 10), ropeColor, true);
		for(int i=-20; i<20; i+=10) DrawLine(new Vector2(i, -5), new Vector2(i+5, 5), new Color(0,0,0,0.3f), 1.0f);
	}
	
	private void DrawCapacitor()
	{
		DrawLine(new Vector2(-30, 0), new Vector2(30, 0), C_ALUMINIUM, 2.0f);
		Rect2 body = new Rect2(-20, -15, 40, 30);
		DrawRect(body, new Color(0.1f, 0.15f, 0.2f), true);
		DrawRect(body, C_ALUMINIUM, false, 1.0f);
		DrawRect(new Rect2(10, -15, 8, 30), new Color(0.8f, 0.8f, 0.8f, 0.8f), true);
		DrawString(_defaultFont, new Vector2(11, 5), "-", HorizontalAlignment.Left, -1, 16, Colors.Black);
		DrawString(_defaultFont, new Vector2(-18, 5), "470µF", HorizontalAlignment.Left, -1, 10, Colors.White);
	}

	private void DrawFuse()
	{
		Rect2 tube = new Rect2(-25, -10, 50, 20);
		DrawRect(tube, new Color(1, 1, 1, 0.1f), true);
		DrawRect(tube, C_ALUMINIUM, false, 1.5f);
		DrawRect(new Rect2(-25, -10, 10, 20), C_ALUMINIUM, true);
		DrawRect(new Rect2(15, -10, 10, 20), C_ALUMINIUM, true);
		DrawLine(new Vector2(-15, 0), new Vector2(15, 0), C_COPPER, 1.5f);
	}

	private void DrawSwitch()
	{
		DrawRect(new Rect2(-20, -20, 40, 40), C_DARK_BASE, true);
		DrawRect(new Rect2(-10, -15, 20, 30), Color.FromHtml("#00ff00"), true);
		DrawRect(new Rect2(-10, -15, 20, 30), Colors.Black, false, 1.0f);
	}

	private void DrawNail()
	{
		Color nailColor = Color.FromHtml("#778");
		DrawRect(new Rect2(-25, -3, 50, 6), nailColor, true);
		DrawRect(new Rect2(-25, -6, 5, 12), nailColor, true);
	}

	private void DrawCoil()
	{
		for(int i=0; i<5; i++) DrawArc(new Vector2(-20 + i*10, 0), 6, 0, 6.28f, 16, C_COPPER, 2.0f);
	}

	private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				if (mouseEvent.Pressed)
				{
					if (IsAnyDragging) return;
					_dragging = true;
					IsAnyDragging = true;
					GetViewport().SetInputAsHandled();

					if (CurrentSlot != null)
					{
						CurrentSlot.OccupyingItem = null;
						CurrentSlot = null;
					}
					ZIndex = 10;
					QueueRedraw();
				}
				else
				{
					if (_dragging)
					{
						_dragging = false;
						IsAnyDragging = false;
						ZIndex = 0;
						EmitSignal(SignalName.DragEnded, this);
						QueueRedraw();
					}
				}
			}
		}
	}

	public void ReturnToStart()
	{
		GlobalPosition = _originalPosition;
		CurrentSlot = null;
		QueueRedraw();
	}
}
