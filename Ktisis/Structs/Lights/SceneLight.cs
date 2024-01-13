using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Ktisis.Structs.Lights;

[StructLayout(LayoutKind.Explicit, Size = 0xA0)]
public struct SceneLight {
	[FieldOffset(0x00)] public unsafe nint* _vf;
	
	[FieldOffset(0x00)] public DrawObject DrawObject;

	[FieldOffset(0x50)] public Transform Transform;

	[FieldOffset(0x80)] public nint Culling;

	[FieldOffset(0x88)] public byte Flags00; // 1 = Visible
	[FieldOffset(0x89)] public byte Flags01; // 1 = UpdateMaterials

	[FieldOffset(0x90)] public unsafe RenderLight* RenderLight;
}
