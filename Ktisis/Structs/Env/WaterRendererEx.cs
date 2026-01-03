using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Ktisis.Structs.Env;

[StructLayout(LayoutKind.Explicit, Size = 0x570)]
public struct WaterRendererEx {
	[FieldOffset(0x000)] public WaterRenderer _base;
	[FieldOffset(0x140)] public float Unk1;
	[FieldOffset(0x144)] public float Unk2;
	[FieldOffset(0x148)] public float Unk3;
	[FieldOffset(0x14C)] public float Unk4;
}
