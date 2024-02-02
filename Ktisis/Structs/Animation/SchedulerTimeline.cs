using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.System.Scheduler.Resource;

using Ktisis.Structs.Common;

namespace Ktisis.Structs.Animation;

[StructLayout(LayoutKind.Explicit, Size = 0x274)]
public struct SchedulerTimeline {
	[FieldOffset(0)] public TimelineController Controller;

	[FieldOffset(0x090)] public unsafe TimelineGroup* TimelineGroup;
	[FieldOffset(0x098)] public unsafe SchedulerResource* SchedulerResource;

	[FieldOffset(0x0A8)] public unsafe char* FilePath1;
	[FieldOffset(0x0B0)] public unsafe char* FilePath2;

	[FieldOffset(0x0D8)] public ObjectUnion UnkObject1;
	[FieldOffset(0x0F0)] public ObjectUnion UnkObject2;

	[FieldOffset(0x170)] public unsafe byte* UnkData;

	[FieldOffset(0x180)] public unsafe Handle* TimelineHandle;

	[FieldOffset(0x18C)] public uint ObjectIndex;
	[FieldOffset(0x190)] public uint TargetIndex;

	[FieldOffset(0x224)] public unsafe fixed char FilePathBuffer[40];
	
	[StructLayout(LayoutKind.Sequential, Size = 0x10)]
	public struct Handle {
		public unsafe SchedulerTimeline* Data;
		public uint Flags;
	}
}
