using System.Runtime.InteropServices;

// Naming is probably wildly off here but this is barely used right now.

namespace Ktisis.Structs.Havok {
	[StructLayout(LayoutKind.Explicit, Size = 0x1C0)]
	public unsafe struct HkaIndex {
		[FieldOffset(0x12C)] public short BoneNodeIndex;
		[FieldOffset(0x12E)] public short BoneParentIndex;

		[FieldOffset(0x140), MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public void* Poses;
	}
}