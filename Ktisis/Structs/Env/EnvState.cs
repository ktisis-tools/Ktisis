using System.Runtime.InteropServices;

using Ktisis.Structs.Env.Weather;

namespace Ktisis.Structs.Env;

[StructLayout(LayoutKind.Explicit, Size = 0x258)]
public struct EnvState {
	[FieldOffset(0x008)] public uint SkyId;
	
	[FieldOffset(0x020)] public EnvLighting Lighting;
	[FieldOffset(0x094)] public EnvStars Stars;
	[FieldOffset(0x0BC)] public EnvFog Fog;
	
	[FieldOffset(0x104)] public EnvClouds Clouds;
	[FieldOffset(0x12C)] public EnvRain Rain;
	[FieldOffset(0x160)] public EnvDust Dust;
	[FieldOffset(0x194)] public EnvWind Wind;
}
