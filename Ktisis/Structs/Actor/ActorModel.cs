using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit, Size = 0x2B4)]
	public unsafe struct ActorModel {
		[FieldOffset(0)] public Object Object;

		[FieldOffset(0x050)] public Vector3 Position;
		[FieldOffset(0x060)] public Quaternion Rotation;
		[FieldOffset(0x070)] public Vector3 Scale;

		[FieldOffset(0x26C)] public float Height;

		[FieldOffset(0x0A0)] public HkaIndexVec* HkaIndex;
	}

	public unsafe struct HkaIndexVec {
		public short Count;
		public void* Handle;
	}
}