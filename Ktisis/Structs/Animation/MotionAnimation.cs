using System.Runtime.InteropServices;

using Ktisis.Structs.Animation.Clips;

namespace Ktisis.Structs.Animation;

[StructLayout(LayoutKind.Explicit, Size = 0x60)]
public struct MotionAnimation {
	[FieldOffset(0)] public unsafe nint* __vfTable;

	[FieldOffset(0x20)] public unsafe AnimationControl.Handle* AnimationControls;
	[FieldOffset(0x28)] public unsafe MotionControl* ParentControl;
	[FieldOffset(0x30)] public unsafe HavokAnimationClip* ParentClip;
}
