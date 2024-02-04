using System.Runtime.InteropServices;

using Ktisis.Structs.Vfx;

namespace Ktisis.Structs.Animation.Clips;

[StructLayout(LayoutKind.Explicit, Size = 0x188)]
public struct VfxClip {
	[FieldOffset(0)] public BaseClip Clip;

	[FieldOffset(0x98)] public unsafe VfxControl* VfxControl;
}
