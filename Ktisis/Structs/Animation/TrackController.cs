using System.Runtime.InteropServices;

using Ktisis.Structs.Common;

namespace Ktisis.Structs.Animation;

[StructLayout(LayoutKind.Explicit)]
public struct TrackController {
	[FieldOffset(0)] public SchedulerState SchedulerState;

	[FieldOffset(0x28)] public PtrList<TimelineTrack> Tracks;
}
