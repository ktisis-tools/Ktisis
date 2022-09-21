using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct TrackData {
		[FieldOffset(8)] public uint Unknown1;
		[FieldOffset(16)] public float X;
		[FieldOffset(20)] public float Y;
		[FieldOffset(24)] public float Z;
		[FieldOffset(32)] public uint Unknown5;
	}
}
