using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit, Size = 0x2B4)]
	public unsafe struct ActorModel {
		[FieldOffset(0)] public Object Object;

		[FieldOffset(0x050)] public Vector3 Position;
		[FieldOffset(0x060)] public Quaternion Rotation;
		[FieldOffset(0x070)] public Vector3 Scale;

		[FieldOffset(0x274)] public float Height;

		[FieldOffset(0x0A0)] public Skeleton* Skeleton;
	}
}