using System;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Ktisis.Structs.Characters;

[StructLayout(LayoutKind.Explicit, Size = Size)]
public struct EquipmentContainer {
	public const int Length = 10;
	public const int Size = sizeof(ulong) * Length;

	[FieldOffset(0x00)] public unsafe fixed byte Bytes[Size];
	
	[FieldOffset(0x00)] public EquipmentModelId Head;
	[FieldOffset(0x08)] public EquipmentModelId Chest;
	[FieldOffset(0x10)] public EquipmentModelId Hands;
	[FieldOffset(0x18)] public EquipmentModelId Legs;
	[FieldOffset(0x20)] public EquipmentModelId Feet;
	[FieldOffset(0x28)] public EquipmentModelId Earring;
	[FieldOffset(0x30)] public EquipmentModelId Necklace;
	[FieldOffset(0x38)] public EquipmentModelId Bracelet;
	[FieldOffset(0x40)] public EquipmentModelId RingRight;
	[FieldOffset(0x48)] public EquipmentModelId RingLeft;

	public EquipmentModelId this[uint index] {
		get => this.Get(index);
		set => this.Set(index, value);
	}

	private unsafe EquipmentModelId Get(uint index)
		=> *this.GetData(index);

	private unsafe void Set(uint index, EquipmentModelId equip)
		=> *this.GetData(index) = equip;

	public unsafe EquipmentModelId* GetData(uint index) {
		if (index >= Length)
			throw new IndexOutOfRangeException($"Index {index} is out of range (< {Length}).");
		fixed (byte* data = this.Bytes)
			return (EquipmentModelId*)data + index;
	}
}
