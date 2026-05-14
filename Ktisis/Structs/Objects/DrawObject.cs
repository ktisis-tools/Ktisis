using System.Runtime.InteropServices;

namespace Ktisis.Structs.Objects;

[StructLayout(LayoutKind.Explicit, Size = 0x90)]
public struct DrawObject {
	[FieldOffset(0x89)] public OutlineChoice OutlineFlags;
}

public enum OutlineChoice : byte {
	None = 0x03,
	Red = 0x13,
	Green = 0x23,
	Blue = 0x33,
	Yellow = 0x43,
	Orange = 0x53,
	Pink = 0x63
}
