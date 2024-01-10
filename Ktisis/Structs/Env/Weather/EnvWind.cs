using System.Runtime.InteropServices;

namespace Ktisis.Structs.Env.Weather;

[StructLayout(LayoutKind.Explicit, Size = 0x0C)]
public struct EnvWind {
	[FieldOffset(0x00)] public float Direction;
	[FieldOffset(0x04)] public float Angle;
	[FieldOffset(0x08)] public float Speed;
}
