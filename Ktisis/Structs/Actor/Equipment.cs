using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct Equipment {
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
	}
}