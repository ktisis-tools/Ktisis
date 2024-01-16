using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Ktisis.Structs.Characters;

public enum AttachType : uint {
	None = 0,
	Unk1 = 1,
	Unk2 = 2,
	Unk3 = 3,
	Bone = 4
}

[StructLayout(LayoutKind.Explicit)]
public struct Attach {
	[FieldOffset(0x50)] public AttachType Type; // 1-5
	[FieldOffset(0x54)] public uint Capacity;
	[FieldOffset(0x58)] public unsafe Skeleton* ChildSkeleton;
	[FieldOffset(0x60)] public unsafe Skeleton* ParentSkeleton;
	[FieldOffset(0x68)] public uint Count;
	[FieldOffset(0x70)] public unsafe AttachParam* Param;

	public bool IsActive() => this.Type != AttachType.None && this.Count > 0;
}
