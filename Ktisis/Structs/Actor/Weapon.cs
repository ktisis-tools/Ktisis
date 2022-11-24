using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct Weapon {
		[FieldOffset(0x00)] public WeaponEquip Equip;
		[FieldOffset(0x08)] public unsafe WeaponModel* Model;
		[FieldOffset(0x40)] public bool IsSheathed;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct WeaponEquip {
		[FieldOffset(0x00)] public ushort Set;
		[FieldOffset(0x02)] public ushort Base;
		[FieldOffset(0x04)] public ushort Variant;
		[FieldOffset(0x06)] public byte Dye;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct WeaponModel {
		[FieldOffset(0x50)] public hkQsTransformf Transform;
		[FieldOffset(0x50)] public Vector3 Position;
		[FieldOffset(0x60)] public Quaternion Rotation;
		[FieldOffset(0x70)] public Vector3 Scale;

		[FieldOffset(0xA0)] public unsafe Skeleton* Skeleton;
	}
}