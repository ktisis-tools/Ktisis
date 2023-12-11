using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct Equipment {
		public const int SlotCount = 10;
		
		[FieldOffset(0)] public unsafe fixed uint Slots[0x4 * SlotCount];

		[FieldOffset(0x00)] public ItemEquip Head;
		[FieldOffset(0x04)] public ItemEquip Chest;
		[FieldOffset(0x08)] public ItemEquip Hands;
		[FieldOffset(0x0C)] public ItemEquip Legs;
		[FieldOffset(0x10)] public ItemEquip Feet;
		[FieldOffset(0x14)] public ItemEquip Earring;
		[FieldOffset(0x18)] public ItemEquip Necklace;
		[FieldOffset(0x1C)] public ItemEquip Bracelet;
		[FieldOffset(0x20)] public ItemEquip RingRight;
		[FieldOffset(0x24)] public ItemEquip RingLeft;
	}

	[StructLayout(LayoutKind.Explicit, Size = 0x4)]
	public struct ItemEquip {
		[FieldOffset(0)] public ushort Id;
		[FieldOffset(2)] public byte Variant;
		[FieldOffset(3)] public byte Dye;

		public static explicit operator ItemEquip(uint num) => new() {
			Id = (ushort)(num & 0xFFFF),
			Variant = (byte)(num >> 16 & 0xFF),
			Dye = (byte)(num >> 24)
		};
		
		public static explicit operator uint(ItemEquip equip)
			=> (uint)(equip.Id | (equip.Variant << 16) | (equip.Dye << 24));

		public bool Equals(ItemEquip other) => Id == other.Id && Variant == other.Variant;
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
