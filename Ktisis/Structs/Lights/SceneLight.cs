using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Ktisis.Structs.Lights;

[StructLayout(LayoutKind.Explicit, Size = 0xA0)]
public struct SceneLight {
	[FieldOffset(0x00)] public DrawObject DrawObject;

	[FieldOffset(0x90)] public unsafe RenderLight* RenderLight;
}
