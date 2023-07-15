namespace Ktisis.Interface.SceneUi.Logic;

public interface IOverlay {
	public bool CanDraw => true;
	
	public void Draw() { }
}