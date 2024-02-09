namespace Ktisis.Editor.Transforms.Types;

public interface ITransformHandler {
	public ITransformTarget? Target { get; }
	
	public ITransformMemento Begin(ITransformTarget target);
}
