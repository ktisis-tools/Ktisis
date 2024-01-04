using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using RenderLight = Ktisis.Interop.Structs.Render.Light;

namespace Ktisis.Interop.Structs.Scene;

[StructLayout(LayoutKind.Explicit, Size = 0xA0)]
public struct Light {
	[FieldOffset(0x00)] public DrawObject DrawObject;

	[FieldOffset(0x90)] public unsafe RenderLight* RenderLight;
}
