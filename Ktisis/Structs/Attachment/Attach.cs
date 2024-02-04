using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Ktisis.Structs.Attachment;

public enum AttachType : uint {
	None = 0,
	Unk1 = 1,
	Unk2 = 2,
	ElementId = 3,
	BoneIndex = 4
}

[StructLayout(LayoutKind.Explicit)]
public struct Attach {
	[FieldOffset(0x50)] public AttachType Type; // 1-5
	[FieldOffset(0x54)] public uint Capacity;
	[FieldOffset(0x58)] public unsafe Skeleton* Child;
	[FieldOffset(0x60)] public unsafe void* Parent;
	[FieldOffset(0x68)] public uint Count;
	[FieldOffset(0x70)] public unsafe AttachParam* Param;

	public bool IsActive() => this.IsValid() && this.Type != AttachType.None && this.Count > 0;
	public unsafe bool IsValid() => this.Param != null && this.Child != null && this.Parent != null;

	public unsafe Skeleton* GetParentSkeleton() => this.Type switch {
		AttachType.ElementId => ((CharacterBase*)this.Parent)->Skeleton,
		AttachType.BoneIndex => (Skeleton*)this.Parent,
		_ => null // todo
	};
}
