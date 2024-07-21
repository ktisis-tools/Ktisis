using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actors;

public enum PoseModeEnum : byte {
	None = byte.MaxValue,
	Idle = 0,
	Battle = 1,
	SitChair = 2,
	SitGround = 3,
	Sleeping = 4
}

[StructLayout(LayoutKind.Explicit)]
public struct EmoteController {
	[FieldOffset(0x20)] public PoseModeEnum Mode;
	[FieldOffset(0x21)] public byte Pose;
	[FieldOffset(0x35)] public bool IsForceDefaultPose;
	[FieldOffset(0x37)] public bool IsDrawObjectOffset;
}
