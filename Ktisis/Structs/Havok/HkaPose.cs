using System.Runtime.InteropServices;

namespace Ktisis.Structs.Havok {
	[StructLayout(LayoutKind.Explicit, Size = 0x20)]
	public unsafe struct HkaPose {
		[FieldOffset(0x00)] public HkaSkeleton* Skeleton;
		[FieldOffset(0x10)] public ShitVecReversed<Transform> Transforms;
	}
}