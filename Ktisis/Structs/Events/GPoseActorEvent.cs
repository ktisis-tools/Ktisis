using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace Ktisis.Structs.Events;

[StructLayout(LayoutKind.Explicit, Size = 0x110)]
public struct GPoseActorEvent {
	[FieldOffset(0x00)] public unsafe nint* __vfTable;
	[FieldOffset(0x20)] public ulong EntityID;
	[FieldOffset(0xA8)] public unsafe Character* Character;
	[FieldOffset(0xB8)] public uint ObjectID; // DataID or ObjectID depending
	[FieldOffset(0xE0)] public uint _param4;
	[FieldOffset(0xE4)] public uint _param5;
	[FieldOffset(0xE8)] public uint _param6;
	[FieldOffset(0xEC)] public uint CopyObjectIndex;
}
