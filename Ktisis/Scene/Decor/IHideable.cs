namespace Ktisis.Scene.Decor;

public interface IHideable {
	public bool IsHidden { get; set; }

	public void ToggleHidden();
}
