using System.Runtime.InteropServices;

namespace Ktisis.Structs.Vfx;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct VfxResourceHandle {
	[FieldOffset(0)] public ulong Value;
	
	[FieldOffset(0)] public uint Id;
	[FieldOffset(2)] public uint Index;
}
