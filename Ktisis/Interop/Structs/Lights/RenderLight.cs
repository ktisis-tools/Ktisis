using System;
using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace Ktisis.Interop.Structs.Lights;

[Flags]
public enum LightFlags : uint {
	Reflection = 0x01,
	Dynamic = 0x02,
	CharaShadow = 0x04,
	ObjectShadow = 0x08
}

public enum LightType : uint {
	
}

public enum LightMode : uint {
	
}

[StructLayout(LayoutKind.Explicit, Size = 0xA0)]
public struct RenderLight {
	[FieldOffset(0x18)] public LightFlags Flags;
	[FieldOffset(0x1C)] public LightType Type;
	[FieldOffset(0x20)] public unsafe Transform* Transform;
	[FieldOffset(0x28)] public Vector4 Color;
	[FieldOffset(0x68)] public LightMode Mode;
	[FieldOffset(0x8C)] public float Radius;
	[FieldOffset(0x90)] public float CharaShadowRange;
}
