using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Env.Weather;

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public struct EnvFog {
	[FieldOffset(0x00)] public Vector4 Color;
	[FieldOffset(0x10)] public float Distance;
	[FieldOffset(0x14)] public float Thickness;
	[FieldOffset(0x18)] public float _unk1;
	[FieldOffset(0x1C)] public float _unk2;
	[FieldOffset(0x20)] public float Opacity;
	[FieldOffset(0x24)] public float Brightness;
}
