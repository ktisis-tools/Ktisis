using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Env.Weather;

[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public struct EnvLighting {
	[FieldOffset(0x00)] public Vector3 SunLightColor;
	[FieldOffset(0x0C)] public Vector3 MoonLightColor;
	[FieldOffset(0x18)] public Vector3 Ambient;
	[FieldOffset(0x24)] public float _unk1;
	[FieldOffset(0x28)] public float AmbientSaturation;
	[FieldOffset(0x2C)] public float Temperature;
	[FieldOffset(0x30)] public float _unk2;
	[FieldOffset(0x34)] public float _unk3;
	[FieldOffset(0x38)] public float _unk4;
}
