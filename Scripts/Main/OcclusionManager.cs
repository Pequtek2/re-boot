using Godot;
using System.Collections.Generic;

public partial class OcclusionManager : Camera2D
{
	[Export] public Node2D Target; // Tutaj przypisz Gracza w Inspektorze!
	
	// Lista ścian, które aktualnie są ukryte (żebyśmy mogli je przywrócić)
	private Dictionary<long, Wall> _hiddenWalls = new Dictionary<long, Wall>();

public override void _PhysicsProcess(double delta)
	{
		if (Target == null) 
		{
			GD.PrintErr("BŁĄD: Nie przypisałeś Gracza (Target) do skryptu w Kamerze!");
			return;
		}

		var spaceState = GetWorld2D().DirectSpaceState;
		var query = new PhysicsPointQueryParameters2D();
		
		// Celujemy w głowę gracza (dostosuj -30 do wzrostu postaci)
		query.Position = Target.GlobalPosition + new Vector2(0, -30); 
		
		// --- TESTOWANIE WSZYSTKIEGO ---
		// Ustawiamy maskę na 4294967295 (to oznacza "wszystkie warstwy"), 
		// żeby sprawdzić, czy W OGÓLE cokolwiek wykrywa.
		query.CollisionMask = 4294967295; 
		
		var results = spaceState.IntersectPoint(query, 32);

		if (results.Count > 0)
		{
			// GD.Print($"Widzę {results.Count} obiektów na celowniku.");
			foreach (var result in results)
			{
				Node collider = result["collider"].As<Node>();
				// GD.Print($"Trafiłem w obiekt: {collider.Name} na Warstwie: {collider.Get("collision_layer")}");
				
				// Szukamy skryptu Wall w rodzicu
				Wall wallScript = collider.GetParent() as Wall; 
				
				if (wallScript != null)
				{
					 // GD.Print(" -> TO JEST ŚCIANA! Ukrywam ją.");
					 wallScript.SetTransparent(true);
					 // Tu powinna być logika dodawania do listy _hiddenWalls (z poprzedniego kodu)
				}
			}
		}
		else
		{
			// GD.Print("Nic nie zasłania gracza.");
		}
	}
}
