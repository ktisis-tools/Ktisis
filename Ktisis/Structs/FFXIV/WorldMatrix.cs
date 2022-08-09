using System.Runtime.InteropServices;

using FFXIVClientStructs;
using FFXIVClientStructs.Attributes;

namespace Ktisis.Structs.FFXIV {
	[StructLayout(LayoutKind.Explicit, Size = 0x1FC)]
	public unsafe partial struct WorldMatrix {
		[FieldOffset(0x1B4)] public float ProjectionM1;

		[FieldOffset(0x1F4)] public float Width;
		[FieldOffset(0x1F8)] public float Height;
	}
}
