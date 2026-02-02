using Godot;
using System;

public partial class MachineFurnace : Node2D
{
	[Export] public string MachineID = "machine_furnace_1"; // ID do minigry/questów
	[Export] public AnimatedSprite2D Sprite;

	public override void _Ready()
	{
		// Sprawdzaj stan co sekundę (proste rozwiązanie)
		Timer timer = new Timer();
		timer.WaitTime = 1.0f;
		timer.Autostart = true;
		timer.Timeout += CheckState;
		AddChild(timer);

		CheckState(); // Sprawdź też na starcie
	}

	private void CheckState()
	{
		if (MainGameManager.Instance == null) return;

		bool isFixed = MainGameManager.Instance.IsMachineFixed(MachineID);

		// Jeśli naprawiona -> animacja pracy. Jeśli nie -> stoi.
		if (isFixed)
		{
			if (Sprite.Animation != "working") Sprite.Play("working");
		}
		else
		{
			if (Sprite.Animation != "idle") Sprite.Play("idle");
		}
	}
}
