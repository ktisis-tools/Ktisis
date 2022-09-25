using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct ActorGaze {
		[FieldOffset(0x30 + 480 * 0)] public Gaze Torso;
		[FieldOffset(0x30 + 480 * 1)] public Gaze Head;
		[FieldOffset(0x30 + 480 * 2)] public Gaze Eyes;
		[FieldOffset(0x30 + 480 * 3)] public Gaze _Unused;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct Gaze {
		[FieldOffset(8)] public uint Mode; // 0 or 3
		[FieldOffset(16)] public float X;
		[FieldOffset(20)] public float Y;
		[FieldOffset(24)] public float Z;
		[FieldOffset(32)] public uint Unk5;
	}

	public enum GazeControl : int {
		All = -1,
		Torso = 0,
		Head = 1,
		Eyes = 2
	}
}