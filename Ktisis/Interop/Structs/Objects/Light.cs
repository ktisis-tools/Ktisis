using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace Ktisis.Interop.Structs.Objects; 

[StructLayout(LayoutKind.Explicit)]
public struct Light {
	[FieldOffset(0)] public Object Object;
}