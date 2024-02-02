using System.Runtime.InteropServices;

namespace Ktisis.Structs.Animation;

[StructLayout(LayoutKind.Explicit, Size = 0x80)]
public struct TimelineController {
	[FieldOffset(0)] public SchedulerState SchedulerState;

	[FieldOffset(0x18)] public unsafe TrackController* TrackController;
	[FieldOffset(0x20)] public unsafe void* Child;
	[FieldOffset(0x28)] public unsafe byte* Data;
	
	[FieldOffset(0x50)] public uint QueuedClipCount;
	[FieldOffset(0x54)] public uint Flags;
	[FieldOffset(0x58)] public uint Unk1;
	[FieldOffset(0x5C)] public uint Unk2;
}
