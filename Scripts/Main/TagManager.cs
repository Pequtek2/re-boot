using Godot;
using System.Collections.Generic;

public partial class TagManager : Node
{
	public static TagManager Instance { get; private set; }

	// Zbiór unikalnych tagów (np. "kierownik_met", "secret_found")
	private HashSet<string> _tags = new HashSet<string>();

	public override void _Ready()
	{
		Instance = this;
	}

	public void AddTag(string tag)
	{
		if (!_tags.Contains(tag))
		{
			_tags.Add(tag);
			GD.Print($"[TAG] Dodano tag: {tag}");
		}
	}

	public void RemoveTag(string tag)
	{
		if (_tags.Contains(tag)) _tags.Remove(tag);
	}

	public bool HasTag(string tag) => _tags.Contains(tag);
	
	// Reset (np. przy nowej grze)
	public void ClearAll() => _tags.Clear();
}
