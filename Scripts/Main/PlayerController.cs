using Godot;
using System;
using System.Collections.Generic; // Potrzebne do obsługi Listy
using System.Linq;

public partial class PlayerController : CharacterBody2D
{
	[Export]
	public TileMapLayer LevelTileMap;

	[Export]
	public float Speed = 200.0f;
	
	[Export] public Sprite2D TileSelector;
	
	// Nasz nowy mózg do szukania ścieżki w siatce
	private AStarGrid2D _astar;
	
	// Lista punktów, przez które musimy przejść
	private List<Vector2> _currentPath = new List<Vector2>();

	public override void _Ready()
	{
		// Inicjalizacja siatki A* po załadowaniu sceny
		Callable.From(SetupGrid).CallDeferred();
	}
	private void UpdateTileSelector()
	{
		if (TileSelector == null || LevelTileMap == null) return;

		// Pobierz pozycję myszki
		Vector2 globalMousePos = GetGlobalMousePosition();
		
		// Przelicz na współrzędne siatki
		Vector2 localMousePos = LevelTileMap.ToLocal(globalMousePos);
		Vector2I gridCoords = LevelTileMap.LocalToMap(localMousePos);

		// Opcjonalnie: Ukryj kursor, jeśli wyjedziemy poza mapę
		if (LevelTileMap.GetCellSourceId(gridCoords) == -1)
		{
			 TileSelector.Visible = false;
			 return;
		}
		else
		{
			 TileSelector.Visible = true;
		}

		// Przelicz z powrotem na środek kafelka
		Vector2 tileCenterLocal = LevelTileMap.MapToLocal(gridCoords);
		Vector2 tileCenterGlobal = LevelTileMap.ToGlobal(tileCenterLocal);

		// Ustaw pozycję kursora
		TileSelector.GlobalPosition = tileCenterGlobal;
	}
	// Funkcja szukająca najbliższego wolnego kafelka metodą rozchodzenia się falą (BFS)
	private Vector2I GetClosestWalkableTile(Vector2I startNode)
	{
		// Jeśli kliknięty punkt od razu jest wolny, zwróć go
		if (!_astar.IsPointSolid(startNode)) return startNode;

		// Kolejka punktów do sprawdzenia
		Queue<Vector2I> queue = new Queue<Vector2I>();
		queue.Enqueue(startNode);

		// Zbiór odwiedzonych punktów, żeby nie sprawdzać w kółko tych samych
		HashSet<Vector2I> visited = new HashSet<Vector2I>();
		visited.Add(startNode);

		// Zabezpieczenie: szukamy max w promieniu np. 50 kratek, żeby nie zawiesić gry
		int maxIterations = 50; 
		int currentIteration = 0;

		// Kierunki sąsiadów (Góra, Dół, Lewo, Prawo)
		Vector2I[] directions = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };

		while (queue.Count > 0 && currentIteration < maxIterations)
		{
			Vector2I current = queue.Dequeue();
			currentIteration++;

			// Sprawdź sąsiadów
			foreach (var dir in directions)
			{
				Vector2I neighbor = current + dir;

				// Jeśli już tu byliśmy, pomiń
				if (visited.Contains(neighbor)) continue;

				// Sprawdź czy sąsiad jest w granicach mapy
				if (!_astar.Region.HasPoint(neighbor)) continue;

				// KLUCZOWE: Jeśli sąsiad NIE jest ścianą -> Mamy zwycięzcę!
				if (!_astar.IsPointSolid(neighbor))
				{
					return neighbor;
				}

				// Jeśli to nadal ściana, dodaj do kolejki do sprawdzenia w następnym kroku
				visited.Add(neighbor);
				queue.Enqueue(neighbor);
			}
		}

		// Jeśli nic nie znaleziono (np. kliknąłeś w środek oceanu ścian), zwróć punkt startowy (nic się nie stanie)
		return startNode;
	}
	private void SetupGrid()
	{
		if (LevelTileMap == null)
		{
			GD.PrintErr("Brak TileMapy!");
			return;
		}

		_astar = new AStarGrid2D();
		
		// 1. Konfiguracja pod Izometrię
		_astar.Region = LevelTileMap.GetUsedRect();
		_astar.CellSize = LevelTileMap.TileSet.TileSize;
		
		// POPRAWKA JEST TUTAJ: Używamy DiagonalModeEnum
		_astar.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never;
		
		// Ustawiamy heurystykę (sposób liczenia odległości) na Manhattan (kratkowa)
		_astar.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan;
		_astar.DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Manhattan;

		_astar.Update();

// 2. Oznaczanie przeszkód
		for (int x = _astar.Region.Position.X; x < _astar.Region.End.X; x++)
		{
			for (int y = _astar.Region.Position.Y; y < _astar.Region.End.Y; y++)
			{
				var coords = new Vector2I(x, y);
				
				// Pobieramy dane kafelka
				TileData tileData = LevelTileMap.GetCellTileData(coords);

				bool isSolid = false;

				if (tileData == null)
				{
					// Brak kafelka = dziura = przeszkoda
					isSolid = true;
				}
				else
				{
					// Jest kafelek, sprawdzamy czy to ściana w Custom Data
					// "is_wall" to nazwa którą wpisałeś w TileSecie
					isSolid = (bool)tileData.GetCustomData("is_wall");
				}

				// Jeśli solidne, zablokuj punkt w A*
				if (isSolid)
				{
					_astar.SetPointSolid(coords, true);
				}
			}
		}
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

private void MoveToMouse(Vector2 globalMousePos)
	{
		if (LevelTileMap == null || _astar == null) return;

		// 1. Gdzie stoimy?
		Vector2 localPlayerPos = LevelTileMap.ToLocal(GlobalPosition);
		Vector2I startGridPos = LevelTileMap.LocalToMap(localPlayerPos);
		
		// 2. Gdzie kliknęliśmy?
		Vector2 localMousePos = LevelTileMap.ToLocal(globalMousePos);
		Vector2I clickedGridPos = LevelTileMap.LocalToMap(localMousePos);

		// 3. Sprawdź cel. Jeśli to ściana -> Znajdź najbliższą podłogę!
		Vector2I targetGridPos = clickedGridPos;
		
		if (_astar.IsPointSolid(clickedGridPos))
		{
			// Tutaj dzieje się magia
			targetGridPos = GetClosestWalkableTile(clickedGridPos);
			
			// Jeśli funkcja zwróciła to samo (czyli nie znalazła nic wolnego w pobliżu), anuluj
			if (_astar.IsPointSolid(targetGridPos)) return;
		}

		// Zabezpieczenie: Nie idź, jeśli cel to to samo miejsce gdzie stoisz
		if (startGridPos == targetGridPos) return;

		// 4. Wyznacz ścieżkę do NOWEGO celu (targetGridPos)
		var idPath = _astar.GetIdPath(startGridPos, targetGridPos);
		
		_currentPath.Clear();
		foreach (Vector2I id in idPath)
		{
			Vector2 worldPos = LevelTileMap.ToGlobal(LevelTileMap.MapToLocal(id));
			_currentPath.Add(worldPos);
		}
		
		if (_currentPath.Count > 0) _currentPath.RemoveAt(0);
	}
	public override void _Process(double delta)
	{
		// _Process wykonuje się co klatkę graficzną - idealne do UI/Kursora
		UpdateTileSelector();
	}
	public override void _PhysicsProcess(double delta)
	{
		if (_currentPath.Count == 0)
		{
			Velocity = Vector2.Zero;
			return;
		}

		// Pobierz następny cel z listy
		Vector2 targetPos = _currentPath[0];
		float distance = GlobalPosition.DistanceTo(targetPos);

		if (distance > 2.0f)
		{
			// Idź w stronę celu
			Velocity = GlobalPosition.DirectionTo(targetPos) * Speed;
			MoveAndSlide();
		}
		else
		{
			// Doszliśmy do środka kafelka -> usuń go z listy i idź do następnego
			GlobalPosition = targetPos; // "Przyklej" idealnie do środka
			_currentPath.RemoveAt(0);
		}
	}
}
