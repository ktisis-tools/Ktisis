namespace Ktisis.Scene.Decor.Ik;

public interface IIkNode {
	public bool IsEnabled { get; }
	
	public void Enable();
	public void Disable();
}
