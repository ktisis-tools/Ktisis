using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Env.Weather;

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public struct EnvClouds {
	[FieldOffset(0x00)] public Vector3 CloudColor;
	[FieldOffset(0x0C)] public Vector3 Color2;
	[FieldOffset(0x18)] public float Gradient;
	[FieldOffset(0x1C)] public float SideHeight;
	[FieldOffset(0x20)] public uint CloudTexture;
	[FieldOffset(0x24)] public uint CloudSideTexture;
}
