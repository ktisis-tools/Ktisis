using System.Numerics;

namespace Ktisis.Scene.Interfaces {
	public interface IGizmoTransform : ITransformable {
		public abstract Matrix4x4? GetMatrix();
		public abstract void SetMatrix(Matrix4x4 mx);
	}
}