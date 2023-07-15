using System.Runtime.InteropServices;

using CSModel = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Model;

namespace Ktisis.Interop.Structs.Objects;

[StructLayout(LayoutKind.Explicit)]
public struct Model {
	[FieldOffset(0)] public CSModel Base;

	[FieldOffset(0x58)] public unsafe ModelObject* Object;
	[FieldOffset(0x60)] public int ObjectCount;
}
