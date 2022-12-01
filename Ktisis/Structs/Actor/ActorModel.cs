using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct ActorModel {
		[FieldOffset(0)] public Object Object;

		[FieldOffset(0x050)] public hkQsTransformf Transform;
		[FieldOffset(0x050)] public Vector3 Position;
		[FieldOffset(0x060)] public Quaternion Rotation;
		[FieldOffset(0x070)] public Vector3 Scale;

		[FieldOffset(0x0A0)] public unsafe Skeleton* Skeleton;

		[FieldOffset(0x148)] public unsafe Breasts* Bust;

		[FieldOffset(0x274)] public float Height;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct Breasts {
		[FieldOffset(0x68)] public Vector3 Scale;
	}
}