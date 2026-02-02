using Godot;
using System;

public partial class MachineAnimator : Node
{
	[Export] public string MachineID = ""; // Tu wpisz ID z Questa (np. machine_east_1)
	
	[Export] public AnimatedSprite2D TargetSprite; // Przeciągnij tu AnimatedSprite maszyny

	[Export] public string AnimWorking = "working"; // Nazwa animacji, gdy działa
	[Export] public string AnimBroken = "idle";     // Nazwa animacji, gdy zepsute

	// Timer, żeby nie sprawdzać stanu w każdej klatce (optymalizacja)
	private Timer _checkTimer;

	public override void _Ready()
	{
		// 1. Zabezpieczenie: Jeśli zapomniałeś przypisać Sprite, spróbuj znaleźć go u Rodzica
		if (TargetSprite == null)
		{
			TargetSprite = GetParent().GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		}

		// 2. Ustawiamy timer sprawdzający stan co 0.5 sekundy
		_checkTimer = new Timer();
		_checkTimer.WaitTime = 0.5f;
		_checkTimer.Autostart = true;
		_checkTimer.Timeout += CheckState;
		AddChild(_checkTimer);

		// 3. Sprawdź od razu na starcie
		CheckState();
	}

	private void CheckState()
	{
		if (TargetSprite == null) return;

		// Jeśli nie wpisałeś ID, uznajemy, że to dekoracja i ma działać zawsze
		if (string.IsNullOrEmpty(MachineID))
		{
			PlayAnimSafe(AnimWorking);
			return;
		}

		// Sprawdzamy w Managerze czy naprawione
		bool isFixed = false;
		if (MainGameManager.Instance != null)
		{
			isFixed = MainGameManager.Instance.IsMachineFixed(MachineID);
		}

		// Wybór animacji
		if (isFixed)
			PlayAnimSafe(AnimWorking);
		else
			PlayAnimSafe(AnimBroken);
	}

	private void PlayAnimSafe(string animName)
	{
		// Sprawdź czy taka animacja w ogóle istnieje w SpriteFrames
		if (TargetSprite.SpriteFrames == null || !TargetSprite.SpriteFrames.HasAnimation(animName))
			return;

		// Jeśli ta animacja już leci, to jej nie resetuj (żeby nie migała)
		if (TargetSprite.Animation != animName || !TargetSprite.IsPlaying())
		{
			TargetSprite.Play(animName);
		}
	}
}
