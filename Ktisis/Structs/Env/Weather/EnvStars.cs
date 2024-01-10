using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Env.Weather;

[StructLayout(LayoutKind.Explicit, Size = 0x28)]
public struct EnvStars {
	[FieldOffset(0x00)] public float ConstellationIntensity;
	[FieldOffset(0x04)] public float Constellations;
	[FieldOffset(0x08)] public float Stars;
	[FieldOffset(0x0C)] public float GalaxyIntensity;
	[FieldOffset(0x10)] public float StarIntensity;
	[FieldOffset(0x14)] public Vector4 MoonColor;
	[FieldOffset(0x24)] public float MoonBrightness;
}
