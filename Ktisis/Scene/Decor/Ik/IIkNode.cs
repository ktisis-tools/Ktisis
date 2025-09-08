namespace Ktisis.Scene.Decor.Ik;

public interface IIkNode {
	public bool IsEnabled { get; }
	
	public void Enable();
	public void Disable();

	public virtual void Toggle() {
		if (this.IsEnabled)
			this.Disable();
		else
			this.Enable();
	}
}
