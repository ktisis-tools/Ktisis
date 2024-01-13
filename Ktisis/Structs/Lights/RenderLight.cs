using System;
using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Graphics;

using Ktisis.Structs.Common;

namespace Ktisis.Structs.Lights;

[Flags]
public enum LightFlags : uint {
	Reflection = 0x01,
	Dynamic = 0x02,
	CharaShadow = 0x04,
	ObjectShadow = 0x08
}

public enum LightType : uint {
	Directional = 1,
	PointLight = 2,
	SpotLight = 3,
	AreaLight = 4
}

public enum FalloffType : uint {
	Linear = 0,
	Quadratic = 1,
	Cubic = 2
}

[StructLayout(LayoutKind.Explicit, Size = 0xA0)]
public struct RenderLight {
	[FieldOffset(0x18)] public LightFlags Flags;
	[FieldOffset(0x1C)] public LightType LightType;
	[FieldOffset(0x20)] public unsafe Transform* Transform;
	[FieldOffset(0x28)] public ColorHDR Color;
	[FieldOffset(0x38)] public Vector3 _unkVec0;
	[FieldOffset(0x44)] public Vector3 _unkVec1;
	[FieldOffset(0x50)] public Vector4 _unkVec2;
	[FieldOffset(0x60)] public float ShadowNear;
	[FieldOffset(0x64)] public float ShadowFar;
	[FieldOffset(0x68)] public FalloffType FalloffType;
	[FieldOffset(0x70)] public Vector2 AreaAngle;
	[FieldOffset(0x78)] public float _unk0;
	[FieldOffset(0x80)] public float Falloff;
	[FieldOffset(0x84)] public float LightAngle; // 0-90deg
	[FieldOffset(0x88)] public float FalloffAngle; // 0-90deg
	[FieldOffset(0x8C)] public float Range;
	[FieldOffset(0x90)] public float CharaShadowRange;
}
