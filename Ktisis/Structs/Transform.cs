using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs {
	public struct Transform {
		public Vector4 Translate;
		public Quaternion Rotate;
		public Vector4 Scale;
	}
}
