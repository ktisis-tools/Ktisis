using System.Runtime.InteropServices;

namespace Ktisis.Structs.Animation.Clips;

[StructLayout(LayoutKind.Explicit, Size = 0xD0)]
public struct HavokAnimationClip {
	[FieldOffset(0x00)] public BaseClip Clip;
	
	[FieldOffset(0x98)] public unsafe MotionControl* MotionControl;
	[FieldOffset(0xA0)] public unsafe char* AnimationName;
}
