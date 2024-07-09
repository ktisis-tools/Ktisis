using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Ktisis.Structs.Events;

[StructLayout(LayoutKind.Explicit, Size = 0x130)]
public struct GPoseActorEvent {
	[FieldOffset(0x00)] public unsafe nint* __vfTable;
	[FieldOffset(0x20)] public ulong EntityID;
	[FieldOffset(0xD0)] public unsafe Character* Character;
	[FieldOffset(0xE0)] public uint ObjectID; // DataID or ObjectID depending
	[FieldOffset(0x108)] public uint _param4;
	[FieldOffset(0x10C)] public uint _param5;
	[FieldOffset(0x110)] public uint _param6;
	[FieldOffset(0x114)] public uint CopyObjectIndex;
}
