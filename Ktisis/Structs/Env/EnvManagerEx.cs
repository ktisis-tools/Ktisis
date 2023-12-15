using System.Runtime.InteropServices;

namespace Ktisis.Structs.Env {
	[StructLayout(LayoutKind.Explicit, Size = 0x900)]
	public struct EnvManagerEx {
		[FieldOffset(0x08)] public unsafe EnvSceneEx* EnvScene;
		
		[FieldOffset(0x10)] public float Time;

		[FieldOffset(0x27)] public byte ActiveWeather;
		
		[FieldOffset(0x58)] public uint SkyId;
	}
}
