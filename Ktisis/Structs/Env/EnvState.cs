using System.Runtime.InteropServices;

using Ktisis.Structs.Env.Weather;

namespace Ktisis.Structs.Env;

[StructLayout(LayoutKind.Explicit, Size = 0x2F8)]
public struct EnvState {
	[FieldOffset(0x008)] public uint SkyId;
	
	[FieldOffset(0x020)] public EnvLighting Lighting;
	[FieldOffset(0x098)] public EnvStars Stars;
	[FieldOffset(0x0C0)] public EnvFog Fog;
	
	[FieldOffset(0x148)] public EnvClouds Clouds;
	[FieldOffset(0x170)] public EnvRain Rain;
	[FieldOffset(0x1A4)] public EnvDust Dust;
	[FieldOffset(0x1D8)] public EnvWind Wind;
}
