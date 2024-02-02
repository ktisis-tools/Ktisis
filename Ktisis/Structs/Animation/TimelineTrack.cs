using System.Runtime.InteropServices;

using Ktisis.Structs.Animation.Clips;
using Ktisis.Structs.Common;

namespace Ktisis.Structs.Animation;

[StructLayout(LayoutKind.Explicit)]
public struct TimelineTrack {
	[FieldOffset(0)] public SchedulerState SchedulerState;

	[FieldOffset(0x18)] public PtrList<BaseClip> Clips;

	[FieldOffset(0x28)] public unsafe byte* ResourceData;
}
