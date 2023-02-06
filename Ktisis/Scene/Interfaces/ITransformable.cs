using System.Numerics;

using Ktisis.Posing;

namespace Ktisis.Scene.Interfaces {
	public interface ITransformable {
		public abstract Transform? GetTransform();
		public abstract void SetTransform(Transform trans);
	}
}