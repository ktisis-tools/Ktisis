using System.Runtime.InteropServices;

namespace Ktisis.Structs.Vfx.Apricot;

[StructLayout(LayoutKind.Explicit, Size = 0x4A8)]
public struct ApricotInstance {
	[FieldOffset(0x18C)] public float F1;
	[FieldOffset(0x1BC)] public float F2;
	[FieldOffset(0x1D4)] public float F3;
	[FieldOffset(0x1F4)] public float F4;
	[FieldOffset(0x49D)] public byte State;
}
