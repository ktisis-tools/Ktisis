using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

namespace Ktisis.Interop.Structs.Objects; 

[StructLayout(LayoutKind.Explicit, Size = 0xD0)]
public struct ModelObject {
	[FieldOffset(0x50)] public Vector3 Position;
	[FieldOffset(0x60)] public Quaternion Rotation;
	[FieldOffset(0x70)] public Vector3 Scale;
	
	[FieldOffset(0xC0)] public unsafe Skeleton* Skeleton;
}