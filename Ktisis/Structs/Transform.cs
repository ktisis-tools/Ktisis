using System.Numerics;

namespace Ktisis.Structs {
	public struct Transform {
		public Vector4 Translate;
		public Quaternion Rotate;
		public Vector4 Scale;

		public Transform(Vector4 translate, Quaternion rotate, Vector4 scale) {
			Translate = translate;
			Rotate = rotate;
			Scale = scale;
		}
	}
}
