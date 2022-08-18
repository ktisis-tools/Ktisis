using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs {
	[StructLayout(LayoutKind.Explicit, Size = 0x30)]
	public struct Transform {
		[FieldOffset(0x00)]  public Vector4 Position;
		[FieldOffset(0x10)] public Quaternion Rotation;
		[FieldOffset(0x20)] public Vector4 Scale;
		
		public Transform(Vector4 translate, Quaternion rotate, Vector4 scale) {
			Position = translate;
			Rotation = rotate;
			Scale = scale;
		}
	}
}
