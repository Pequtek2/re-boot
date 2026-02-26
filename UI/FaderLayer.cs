using Godot;
using System.Threading.Tasks;

public partial class FaderLayer : CanvasLayer
{
	[Export] public NodePath FadeRectPath = "FadeRect";
	private ColorRect _fadeRect;

	public override void _Ready()
	{
		_fadeRect = GetNode<ColorRect>(FadeRectPath);
	}

	public async Task FadeOut(float duration = 0.35f)
	{
		var t = CreateTween();
		t.TweenProperty(_fadeRect, "color", new Color(0,0,0,1), duration);
		await ToSignal(t, Tween.SignalName.Finished);
	}

	public async System.Threading.Tasks.Task FadeIn(float duration = 0.35f)
	{
		// start: czarny
		_fadeRect.Color = new Color(0, 0, 0, 1);

		var t = CreateTween();
		t.TweenProperty(_fadeRect, "color", new Color(0, 0, 0, 0), duration)
		 .SetTrans(Tween.TransitionType.Cubic)
		 .SetEase(Tween.EaseType.Out);

		await ToSignal(t, Tween.SignalName.Finished);
	}
}
