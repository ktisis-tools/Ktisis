using System.Runtime.InteropServices;

using Ktisis.Structs.Animation.Clips;

namespace Ktisis.Structs.Animation;

[StructLayout(LayoutKind.Explicit, Size = 0x80)]
public struct MotionControl {
	[FieldOffset(0x44)] public uint FrameCount;
	[FieldOffset(0x4C)] public float StartSpeed;
	[FieldOffset(0x54)] public float PlaySpeed;

	[FieldOffset(0x60)] public unsafe HavokAnimationClip* ParentClip;

	[FieldOffset(0x78)] public unsafe MotionAnimation* Animation;
}
