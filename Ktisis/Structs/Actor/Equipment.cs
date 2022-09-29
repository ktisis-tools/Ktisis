using System;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct Equipment {
		[FieldOffset(0)] public unsafe fixed uint Slots[0x4 * 10];

		[FieldOffset(0x00)] public EquipItem Head;
		[FieldOffset(0x04)] public EquipItem Chest;
		[FieldOffset(0x08)] public EquipItem Hands;
		[FieldOffset(0x0C)] public EquipItem Legs;
		[FieldOffset(0x10)] public EquipItem Feet;
		[FieldOffset(0x14)] public EquipItem Earring;
		[FieldOffset(0x18)] public EquipItem Necklace;
		[FieldOffset(0x1C)] public EquipItem Bracelet;
		[FieldOffset(0x20)] public EquipItem RingRight;
		[FieldOffset(0x24)] public EquipItem RingLeft;
	}

	[StructLayout(LayoutKind.Explicit, Size = 0x4)]
	public struct EquipItem {
		[FieldOffset(0)] public ushort Id;
		[FieldOffset(2)] public byte Variant;
		[FieldOffset(3)] public byte Dye;

		public static explicit operator EquipItem(uint num) => new() {
			Id = (ushort)(num & 0xFFFF),
			Variant = (byte)(num >> 16 & 0xFF),
			Dye = (byte)(num >> 24)
		};

		public bool Equals(EquipItem other) => Id == other.Id && Variant == other.Variant;
	}

	public enum EquipIndex : uint {
		Head,
		Chest,
		Hands,
		Legs,
		Feet,
		Earring,
		Necklace,
		Bracelet,
		RingRight,
		RingLeft
	}
}