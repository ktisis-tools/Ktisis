using System.Runtime.InteropServices;

using Ktisis.Structs.Animation;

namespace Ktisis.Structs.Actors;

[StructLayout(LayoutKind.Explicit, Size = 0x1F0)]
public struct AnimationTimeline {
	[FieldOffset(0)] public unsafe nint** __vfTable;

	[FieldOffset(0x070)] public unsafe fixed ulong SchedulerTimelines[14];
	[FieldOffset(0x0E0)] public unsafe fixed ushort TimelineIds[14];
	[FieldOffset(0x0FC)] public unsafe fixed ushort CurrentTimelineIds[14];
	[FieldOffset(0x118)] public unsafe fixed ushort PreviousTimelineIds[14];
	[FieldOffset(0x154)] public unsafe fixed float TimelineSpeeds[14];

	[FieldOffset(0x18C)] public unsafe fixed float TimelineWeights[14];

	[FieldOffset(0x2D0)] public ushort ActionTimelineId;

	public unsafe SchedulerTimeline* GetSchedulerTimeline(int slot) {
		var value = this.SchedulerTimelines[slot];
		if (this.SchedulerTimelines[slot] == 0)
			return null;
		var ptr = (SchedulerTimeline.Handle*)value;
		return ptr->Flags != 0 ? ptr->Data : null;
	}
}
