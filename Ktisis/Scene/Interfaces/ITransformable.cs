namespace Ktisis.Scene.Interfaces {
	public interface ITransformable {
		// TODO: Unified class for transform types?
		public abstract object? GetTransform();
		public abstract void SetTransform(object trans);
	}
}