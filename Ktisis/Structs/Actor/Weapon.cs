using System;
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
		[FieldOffset(0x60)] public WeaponFlags Flags;

		public unsafe WeaponEquip GetEquip()
			=> this.Model != null ? this.Model->Equip : this.Equip;

		public unsafe void SetEquip(WeaponEquip item) {
			if (this.Model != null)
				this.Model->Equip = item;
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct WeaponEquip {
		[FieldOffset(0x00)] public ushort Set;
		[FieldOffset(0x02)] public ushort Base;
		[FieldOffset(0x04)] public ushort Variant;
		[FieldOffset(0x06)] public ushort Dye;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct WeaponModel {
		[FieldOffset(0x50)] public hkQsTransformf Transform;
		[FieldOffset(0x50)] public Vector3 Position;
		[FieldOffset(0x60)] public Quaternion Rotation;
		[FieldOffset(0x70)] public Vector3 Scale;
		
		[FieldOffset(0x88)] public byte Flags;

		[FieldOffset(0xA0)] public unsafe Skeleton* Skeleton;

		[FieldOffset(0x8F0)] public WeaponEquip Equip;
	}

	public enum WeaponSlot {
		MainHand = 0,
		OffHand = 1,
		Prop = 2
	}

	[Flags]
	public enum WeaponFlags : byte {
		None = 0,
		Hidden = 2
	}
}
