namespace Ktisis.Actions.Types;

public interface IMemento {
	public void Restore();
	public void Apply();
}
