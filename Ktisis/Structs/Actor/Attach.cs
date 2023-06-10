using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Ktisis.Structs.Actor {
	[StructLayout(LayoutKind.Explicit, Size = 0x78)]
	public struct Attach {
		[FieldOffset(0x50)] public uint Type; // 1-5
		[FieldOffset(0x68)] public uint Count;
		[FieldOffset(0x70)] public unsafe BoneAttach* BoneAttach;

		[FieldOffset(0x58)] public unsafe Skeleton* TargetSkeleton;
		[FieldOffset(0x60)] public unsafe Skeleton* ParentSkeleton;
	}

	[StructLayout(LayoutKind.Explicit, Size = 0x68)]
	public struct BoneAttach {
		[FieldOffset(0x02)] public ushort BoneId;

		[FieldOffset(0x30)] public float Scale;
	}
}