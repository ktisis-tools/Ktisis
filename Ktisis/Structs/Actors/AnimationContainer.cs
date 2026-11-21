using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actors;

[StructLayout(LayoutKind.Explicit, Size = 0x350)]
public struct AnimationContainer {
	public const int TimelineOffset = 0x10;
	
	[FieldOffset(0x00)] public unsafe nint** __vfTable;
	[FieldOffset(0x08)] public unsafe CharacterEx* Character;
	[FieldOffset(TimelineOffset)] public AnimationTimeline Timeline;
	[FieldOffset(0x2E0)] public ushort ActionTimelineId;
	[FieldOffset(0x2E6)] public ushort BaseOverride;
}
