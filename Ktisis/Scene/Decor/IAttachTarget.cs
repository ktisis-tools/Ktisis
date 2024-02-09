namespace Ktisis.Scene.Decor;

public interface IAttachTarget {
	public bool TryAcceptAttach(IAttachable child);
}
