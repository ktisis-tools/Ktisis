using Ktisis.Editor.Posing.Ik;

namespace Ktisis.Scene.Decor;

public interface ITwoJointsNode {
	public bool IsEnabled { get; }
	
	public TwoJointsGroup Group { get; }

	public void Enable();
	public void Disable();
	public void Toggle();
}
