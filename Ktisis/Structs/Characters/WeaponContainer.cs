using System;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Ktisis.Structs.Characters;

[StructLayout(LayoutKind.Explicit, Size = Size)]
public struct WeaponContainer {
	public const int Length = 3;
	public const int Size = sizeof(uint) * Length;

	[FieldOffset(0x00)] public unsafe fixed byte Bytes[Size];

	[FieldOffset(0x00)] public WeaponModelId MainHand;
	[FieldOffset(0x04)] public WeaponModelId OffHand;
	[FieldOffset(0x08)] public WeaponModelId Prop;
	
	public WeaponModelId this[uint index] {
		get => this.Get(index);
		set => this.Set(index, value);
	}

	private unsafe WeaponModelId Get(uint index)
		=> *this.GetData(index);

	private unsafe void Set(uint index, WeaponModelId equip)
		=> *this.GetData(index) = equip;

	public unsafe WeaponModelId* GetData(uint index) {
		if (index >= Length)
			throw new IndexOutOfRangeException($"Index {index} is out of range (< {Length}).");
		fixed (byte* data = this.Bytes)
			return (WeaponModelId*)data + index;
	}
}
