using Godot;
using System;
using System.Collections.Generic;

public partial class LogisticsPath : Node2D
{
	[Export] public string ConnectedMachineID = ""; // ID maszyny, która zasila ten taśmociąg (np. "machine_east_1")

	[Export] public Texture2D ItemTexture; 
	[Export] public float Speed = 50.0f;   
	[Export] public float Gap = 40.0f;     

	[Export] public Path2D PathNode;
	[Export] public PathFollow2D TemplateMover; 
	[Export] public Sprite2D TemplateSprite;
	
	// NOWE: Lista animowanych teł (czyli same kafelki taśmociągu)
	[Export] public Godot.Collections.Array<AnimatedSprite2D> BeltVisuals; 

	private List<PathFollow2D> _items = new List<PathFollow2D>();
	private float _pathLength = 0;
	private bool _isRunning = false;

	public override void _Ready()
	{
		// Włączenie Y-Sort dla samego kontenera, żeby dzieci sortowały się poprawnie
		YSortEnabled = true;

		if (PathNode == null || TemplateMover == null) return;

		_pathLength = PathNode.Curve.GetBakedLength();
		if (_pathLength <= 0) return;

		if (TemplateSprite != null && ItemTexture != null)
			TemplateSprite.Texture = ItemTexture;

		TemplateMover.Visible = false; 

		// Generowanie przedmiotów
		int itemCount = Mathf.CeilToInt(_pathLength / Gap);
		for (int i = 0; i < itemCount; i++)
		{
			SpawnItem(i * Gap);
		}

		// Sprawdź stan na starcie
		CheckState();
		
		// Uruchom timer sprawdzający stan (tak jak w piecu)
		Timer timer = new Timer();
		timer.WaitTime = 0.5f;
		timer.Autostart = true;
		timer.Timeout += CheckState;
		AddChild(timer);
	}

	private void SpawnItem(float startProgress)
	{
		var newItem = (PathFollow2D)TemplateMover.Duplicate();
		PathNode.AddChild(newItem);
		newItem.Visible = true;
		newItem.Progress = startProgress;
		newItem.Loop = true;
		_items.Add(newItem);
	}

	private void CheckState()
	{
		// Jeśli nie wpisałeś ID, uznajemy, że taśma działa zawsze (np. początkowa)
		if (string.IsNullOrEmpty(ConnectedMachineID))
		{
			SetRunning(true);
			return;
		}

		// Sprawdź w managerze czy maszyna naprawiona
		bool isFixed = MainGameManager.Instance != null && MainGameManager.Instance.IsMachineFixed(ConnectedMachineID);
		SetRunning(isFixed);
	}

	private void SetRunning(bool run)
	{
		if (_isRunning == run) return; // Nic się nie zmieniło
		_isRunning = run;

		// Obsługa animacji tła (kafelków taśmy)
		if (BeltVisuals != null)
		{
			foreach (var anim in BeltVisuals)
			{
				if (anim == null) continue;
				
				if (_isRunning) anim.Play(); // Start animacji (np. "working")
				else anim.Pause();           // Pauza (lub anim.Play("idle"))
			}
		}
	}

	public override void _Process(double delta)
	{
		// Jeśli zepsute -> nie przesuwaj przedmiotów
		if (!_isRunning) return;

		foreach (var item in _items)
		{
			item.Progress += Speed * (float)delta;
		}
	}
}
