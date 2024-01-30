namespace Ktisis.Scene.Decor;

public interface INodeIk {
	public bool IsEnabled { get; }

	public void Enable();
	public void Disable();
	public void Toggle();
}
