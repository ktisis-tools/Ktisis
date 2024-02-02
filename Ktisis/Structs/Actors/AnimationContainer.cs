using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actors;

[StructLayout(LayoutKind.Explicit, Size = 0x340)]
public struct AnimationContainer {
	[FieldOffset(0x00)] public unsafe nint** __vfTable;
	[FieldOffset(0x08)] public unsafe CharacterEx* Character;
	[FieldOffset(0x10)] public AnimationTimeline Timeline;
}
