using System.Numerics;
using System.Runtime.InteropServices;

namespace Ktisis.Structs.Attach;

public enum ElementId : uint {
	RightShoulder = 0x06,
	LeftShoulder = 0x07,
	RightWrist = 0x0E,
	LeftRight = 0x0F,
	Waist = 0x1F,
	RightHand = 0x20,
	LeftHand = 0x21,
	RightFoot = 0x22,
	LeftFoot = 0x23,
	RightEye = 0x2B,
	LeftEye = 0x2C
}

[StructLayout(LayoutKind.Explicit, Size = 0x40)]
public struct ElementParam {
	[FieldOffset(0x00)] public unsafe fixed char NameBytes[28];
	[FieldOffset(0x20)] public ElementId ElementId;
	[FieldOffset(0x24)] public Vector3 Position;
	[FieldOffset(0x30)] public Vector3 Rotation;
}
