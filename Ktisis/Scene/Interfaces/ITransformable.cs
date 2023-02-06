using Ktisis.Posing;

namespace Ktisis.Scene.Interfaces {
	public interface ITransformable {
		// TODO: Unified class for transform types?
		public abstract Transform? GetTransform();
		public abstract void SetTransform(Transform trans);
	}
}