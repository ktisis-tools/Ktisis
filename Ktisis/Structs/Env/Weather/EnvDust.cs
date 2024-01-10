using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Env.Weather;

// This is responsible for snow and leaves as well as dust, but I'm naming it dust because of its texture path.

[StructLayout(LayoutKind.Explicit, Size = 0x34)]
public struct EnvDust {
	[FieldOffset(0x00)] public float _unk1;
	[FieldOffset(0x04)] public float Intensity;
	[FieldOffset(0x08)] public float Weight;
	[FieldOffset(0x0C)] public float Spread;
	[FieldOffset(0x10)] public float Speed;
	[FieldOffset(0x14)] public float Size;
	[FieldOffset(0x18)] public Vector4 Color;
	[FieldOffset(0x28)] public float Glow;
	[FieldOffset(0x2C)] public float Spin;
	[FieldOffset(0x30)] public uint TextureId;
}
