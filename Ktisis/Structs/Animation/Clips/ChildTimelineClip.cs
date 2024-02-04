using System.Runtime.InteropServices;

namespace Ktisis.Structs.Animation.Clips;

[StructLayout(LayoutKind.Explicit, Size = 0x160)]
public struct ChildTimelineClip {
	[FieldOffset(0)] public BaseClip Clip;

	[FieldOffset(0x0CC)] public float ChildFrame;
	[FieldOffset(0x0D0)] public float PrevChildFrame;

	[FieldOffset(0x128)] public unsafe SchedulerTimeline* ParentTimeline;
	[FieldOffset(0x130)] public unsafe TimelineController* ChildTimeline;
	
	[FieldOffset(0x140)] public unsafe byte* Data;
}
