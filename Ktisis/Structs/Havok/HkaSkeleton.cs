using System.Runtime.InteropServices;

namespace Ktisis.Structs.Havok {
	[StructLayout(LayoutKind.Explicit, Size = 0x38)]
	public unsafe struct HkaSkeleton {
		[FieldOffset(0x18)] public ShitVec<short> ParentIndex;
		[FieldOffset(0x28)] public ShitVec<HkaBone> Bones;
	}
}