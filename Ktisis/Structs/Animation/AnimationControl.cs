using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.Havok;
using FFXIVClientStructs.Interop;
using FFXIVClientStructs.STD;

namespace Ktisis.Structs.Animation;

[StructLayout(LayoutKind.Explicit, Size = 0x00)]
public struct AnimationControl {
	[FieldOffset(0)] public unsafe nint* __vfTable;

	[FieldOffset(0x38)] public unsafe hkaDefaultAnimationControl* HavokControl;

	[StructLayout(LayoutKind.Explicit, Size = 0x28)]
	public struct Handle {
		[FieldOffset(0x00)] public ReferencedClassBase Ref;
		[FieldOffset(0x18)] public StdSet<Pointer<AnimationControl>> Set;
	}
}
