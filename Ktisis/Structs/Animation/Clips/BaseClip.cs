using System.Runtime.InteropServices;

namespace Ktisis.Structs.Animation.Clips;

[StructLayout(LayoutKind.Explicit, Size = 0x98)]
public struct BaseClip {
	[FieldOffset(0)] public unsafe nint* __vfTable;
	[FieldOffset(0)] public SchedulerState SchedulerState;

	[FieldOffset(0x28)] public unsafe TrackController* TrackController;
	[FieldOffset(0x30)] public unsafe TimelineController* ParentTimeline;
	[FieldOffset(0x38)] public unsafe TimelineController* RootTimeline;
	// 0x50 ...
	[FieldOffset(0x48)] public unsafe byte* Data;
	[FieldOffset(0x50)] public float TrackStartFrame;
	[FieldOffset(0x54)] public float TrackTotalFrames;
	[FieldOffset(0x5C)] public float DeltaFrames;
	// 0x70 f32
	[FieldOffset(0x64)] public float ClipStartFrame;
	[FieldOffset(0x68)] public float ClipTotalFrames;

	[FieldOffset(0x84)] public ClipType ClipType;
}
