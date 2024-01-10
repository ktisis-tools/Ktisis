using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Ktisis.Structs.Characters;

[StructLayout(LayoutKind.Explicit, Size = Size)]
public struct Equipment {
	public const int Size = sizeof(uint) * Length;
	public const int Length = 10;

	[FieldOffset(0x00)] public unsafe fixed byte Bytes[Size];
	
	[FieldOffset(0x00)] public EquipmentModelId Head;
	[FieldOffset(0x04)] public EquipmentModelId Chest;
	[FieldOffset(0x08)] public EquipmentModelId Hands;
	[FieldOffset(0x0C)] public EquipmentModelId Legs;
	[FieldOffset(0x10)] public EquipmentModelId Feet;
	[FieldOffset(0x14)] public EquipmentModelId Earring;
	[FieldOffset(0x18)] public EquipmentModelId Necklace;
	[FieldOffset(0x1C)] public EquipmentModelId Bracelet;
	[FieldOffset(0x20)] public EquipmentModelId RingRight;
	[FieldOffset(0x24)] public EquipmentModelId RingLeft;
}
