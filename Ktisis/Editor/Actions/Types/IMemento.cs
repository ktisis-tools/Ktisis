namespace Ktisis.Editor.Actions.Types;

public interface IMemento {
	public void Restore();
	public void Apply();
}
