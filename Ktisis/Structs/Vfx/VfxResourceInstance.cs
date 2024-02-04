using System.Runtime.InteropServices;

namespace Ktisis.Structs.Vfx;

[StructLayout(LayoutKind.Explicit, Size = 0xC0)]
public struct VfxResourceInstance {
	[FieldOffset(0)] public unsafe nint* __vfTable;

	[FieldOffset(0x60)] public VfxResourceHandle Handle;
}
