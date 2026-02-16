using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerController : CharacterBody2D
{
	[Export] public TileMapLayer LevelTileMap;
	[Export] public float Speed = 200.0f;
	
	[Export] public Sprite2D TileSelector;
	[Export] public SpriteFrames MaleFrames;
	[Export] public SpriteFrames FemaleFrames;
	
	private AStarGrid2D _astar;
	private AnimatedSprite2D _animSprite;
	private string _lastAnim = "idle_down";
	
	private List<Vector2> _currentPath = new List<Vector2>();

	public override void _Ready()
	{
		Callable.From(SetupGrid).CallDeferred();
		_animSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		
		if (MainGameManager.Instance != null)
		{
			MainGameManager.Instance.OnSkinChanged += UpdateSkin;
			UpdateSkin(); 
		}
		if (MainGameManager.Instance != null && MainGameManager.Instance.ShouldTeleportPlayer)
	{
		GD.Print($"[Player] Teleportacja na zapisaną pozycję: {MainGameManager.Instance.LastPlayerPosition}");
		
		// Ustawiamy pozycję
		GlobalPosition = MainGameManager.Instance.LastPlayerPosition;
		
		// Resetujemy flagę, żeby przy normalnym restarcie gry nie teleportowało nas na środek
		MainGameManager.Instance.ShouldTeleportPlayer = false;
	}
	}

	public override void _ExitTree()
	{
		if (MainGameManager.Instance != null)
			MainGameManager.Instance.OnSkinChanged -= UpdateSkin;
	}

	private void UpdateSkin()
	{
		if (MainGameManager.Instance == null || _animSprite == null) return;

		if (MainGameManager.Instance.IsMale)
			_animSprite.SpriteFrames = MaleFrames;
		else
			_animSprite.SpriteFrames = FemaleFrames;
			
		_animSprite.Play("idle_down");
	}

	private void SetupGrid()
	{
		if (LevelTileMap == null)
		{
			GD.PrintErr("Brak TileMapy!");
			return;
		}

		_astar = new AStarGrid2D();
		_astar.Region = LevelTileMap.GetUsedRect();
		_astar.CellSize = LevelTileMap.TileSet.TileSize;
		
		// --- ZMIANA: WYMUSZENIE RUCHU 90 STOPNI ---
		// Never = zakaz chodzenia na skos. Postać będzie chodzić "zygzakiem" po kratkach.
		_astar.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never;
		
		// Manhattan = algorytm preferujący proste linie
		_astar.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan;
		_astar.DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Manhattan;
		
		_astar.Update();

		// Oznaczanie ścian
		for (int x = _astar.Region.Position.X; x < _astar.Region.End.X; x++)
		{
			for (int y = _astar.Region.Position.Y; y < _astar.Region.End.Y; y++)
			{
				var coords = new Vector2I(x, y);
				TileData tileData = LevelTileMap.GetCellTileData(coords);

				bool isSolid = false;
				if (tileData == null) isSolid = true;
				else isSolid = (bool)tileData.GetCustomData("is_wall");

				if (isSolid) _astar.SetPointSolid(coords, true);
			}
		}
	}

	public override void _Process(double delta)
	{
		UpdateTileSelector();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_astar == null || LevelTileMap == null) return;

		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
			{
				MoveToMouse(GetGlobalMousePosition());
			}
		}
	}

	private void UpdateTileSelector()
	{
		if (TileSelector == null || LevelTileMap == null) return;

		Vector2 globalMousePos = GetGlobalMousePosition();
		Vector2 localMousePos = LevelTileMap.ToLocal(globalMousePos);
		Vector2I gridCoords = LevelTileMap.LocalToMap(localMousePos);

		if (LevelTileMap.GetCellSourceId(gridCoords) == -1)
		{
			 TileSelector.Visible = false;
			 return;
		}
		
		TileSelector.Visible = true;
		Vector2 tileCenterLocal = LevelTileMap.MapToLocal(gridCoords);
		TileSelector.GlobalPosition = LevelTileMap.ToGlobal(tileCenterLocal);
	}

	private void MoveToMouse(Vector2 globalMousePos)
	{
		if (LevelTileMap == null || _astar == null) return;

		Vector2 localPlayerPos = LevelTileMap.ToLocal(GlobalPosition);
		Vector2I startGridPos = LevelTileMap.LocalToMap(localPlayerPos);
		
		Vector2 localMousePos = LevelTileMap.ToLocal(globalMousePos);
		Vector2I clickedGridPos = LevelTileMap.LocalToMap(localMousePos);

		Vector2I targetGridPos = clickedGridPos;
		
		if (_astar.IsPointSolid(clickedGridPos))
		{
			targetGridPos = GetClosestWalkableTile(clickedGridPos);
			if (_astar.IsPointSolid(targetGridPos)) return;
		}

		if (startGridPos == targetGridPos) return;

		var idPath = _astar.GetIdPath(startGridPos, targetGridPos);
		
		_currentPath.Clear();
		foreach (Vector2I id in idPath)
		{
			Vector2 worldPos = LevelTileMap.ToGlobal(LevelTileMap.MapToLocal(id));
			_currentPath.Add(worldPos);
		}
		
		if (_currentPath.Count > 0) _currentPath.RemoveAt(0);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_currentPath.Count == 0)
		{
			Velocity = Vector2.Zero;
			AnimateMovement(Vector2.Zero);
			return;
		}

		Vector2 targetPos = _currentPath[0];
		float distance = GlobalPosition.DistanceTo(targetPos);

		if (distance > 3.0f)
		{
			Vector2 direction = GlobalPosition.DirectionTo(targetPos);
			Velocity = direction * Speed;
			
			MoveAndSlide();
			AnimateMovement(Velocity.Normalized());
		}
		else
		{
			GlobalPosition = targetPos;
			_currentPath.RemoveAt(0);
		}
	}

	private void AnimateMovement(Vector2 dir)
	{
		string anim = _lastAnim; 

		// 1. CZY STOIMY?
		if (dir.Length() < 0.1f)
		{
			if (_lastAnim.Contains("walk_"))
				anim = _lastAnim.Replace("walk_", "idle_");
			
			if (_animSprite.Animation != anim) _animSprite.Play(anim);
			return;
		}

		// 2. LOGIKA 4 KIERUNKÓW (DOPASOWANA DO IZOMETRII)
		// Teraz, gdy ruch jest "Never Diagonal", wektor zawsze będzie miał wyraźne znaki.
		// Mapowanie według Twoich zdjęć i opisu:
		
		if (dir.X > 0) 
		{
			// PRAWA STRONA EKRANU
			if (dir.Y > 0) 
				anim = "walk_down";   // Prawo-Dół (+X, +Y) -> Przód
			else 
				anim = "walk_right";  // Prawo-Góra (+X, -Y) -> Bok Prawy
		}
		else 
		{
			// LEWA STRONA EKRANU
			if (dir.Y > 0) 
				anim = "walk_left";   // Lewo-Dół (-X, +Y) -> Bok Lewy
			else 
				anim = "walk_up";     // Lewo-Góra (-X, -Y) -> Tył
		}
		
		_animSprite.FlipH = false; // Masz wszystkie klatki, więc flip wyłączony

		// 3. ODTWARZANIE
		if (_animSprite.Animation != anim)
		{
			if (_animSprite.SpriteFrames.HasAnimation(anim))
			{
				_animSprite.Play(anim);
				_lastAnim = anim;
			}
			else
			{
				// Debug w razie literówek
				GD.PrintErr($"BRAK ANIMACJI: '{anim}'");
			}
		}
	}

	private Vector2I GetClosestWalkableTile(Vector2I startNode)
	{
		if (!_astar.IsPointSolid(startNode)) return startNode;

		Queue<Vector2I> queue = new Queue<Vector2I>();
		queue.Enqueue(startNode);
		HashSet<Vector2I> visited = new HashSet<Vector2I>();
		visited.Add(startNode);

		int maxIterations = 50; 
		int currentIteration = 0;
		Vector2I[] directions = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };

		while (queue.Count > 0 && currentIteration < maxIterations)
		{
			Vector2I current = queue.Dequeue();
			currentIteration++;

			foreach (var dir in directions)
			{
				Vector2I neighbor = current + dir;
				if (visited.Contains(neighbor)) continue;
				if (!_astar.Region.HasPoint(neighbor)) continue;

				if (!_astar.IsPointSolid(neighbor)) return neighbor;

				visited.Add(neighbor);
				queue.Enqueue(neighbor);
			}
		}
		return startNode;
	}
}
