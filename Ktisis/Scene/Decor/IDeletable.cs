namespace Ktisis.Scene.Decor;

public interface IDeletable {
	public virtual bool CanDelete => true;
	
	public bool Delete();
}
