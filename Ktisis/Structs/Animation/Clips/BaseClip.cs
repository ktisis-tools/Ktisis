using System.Runtime.InteropServices;

namespace Ktisis.Structs.Animation.Clips;

[StructLayout(LayoutKind.Explicit)]
public struct BaseClip {
	[FieldOffset(0)] public SchedulerState SchedulerState;
}
