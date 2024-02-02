using System.Runtime.InteropServices;

using Ktisis.Structs.Common;

namespace Ktisis.Structs.Animation;

[StructLayout(LayoutKind.Explicit, Size = 0xA78)]
public struct TimelineGroup {
	[FieldOffset(0x00)] public unsafe nint** __vfTable;
	
	[FieldOffset(0x018)] public unsafe SchedulerTimeline* SchedulerTimeline;
	[FieldOffset(0x020)] public unsafe void* Controller;
	[FieldOffset(0x028)] public ObjectUnion Object;
	
	[FieldOffset(0xA6C)] public uint GroupType;
	
	
}
