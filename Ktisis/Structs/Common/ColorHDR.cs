using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Common;

[StructLayout(LayoutKind.Explicit, Size = sizeof(float) * 4)]
public struct ColorHDR {
	[FieldOffset(0x00)] public Vector3 _vec3;
	
	[FieldOffset(0x00)] public float Red;
	[FieldOffset(0x04)] public float Green;
	[FieldOffset(0x08)] public float Blue;
	[FieldOffset(0x0C)] public float Intensity;

	public Vector3 RGB {
		get => Vector3.SquareRoot(this._vec3) / 4.0f;
		set {
			value *= 4.0f;
			this._vec3 = (value * value);
		}
	}
	
	public ColorHDR() {
		this._vec3 = new Vector3(16.0f, 16.0f, 16.0f);
		this.Intensity = 1.0f;
	}
}
