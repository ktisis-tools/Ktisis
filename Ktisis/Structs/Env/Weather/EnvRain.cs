using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Env.Weather;

[StructLayout(LayoutKind.Explicit, Size = 0x34)]
public struct EnvRain {
	[FieldOffset(0x00)] public float Raindrops;
	[FieldOffset(0x04)] public float Intensity;
	[FieldOffset(0x08)] public float Weight;
	[FieldOffset(0x0C)] public float Scatter;
	[FieldOffset(0x10)] public float _unk1;
	[FieldOffset(0x14)] public float Size;
	[FieldOffset(0x18)] public Vector4 Color;
	[FieldOffset(0x28)] public float _unk2;
	[FieldOffset(0x2C)] public float _unk3;
	[FieldOffset(0x30)] public uint _unk4;
}
