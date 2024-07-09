using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actors;

[StructLayout(LayoutKind.Explicit, Size = 0x1BD0)]
public struct CharacterEx {
	[FieldOffset(0x09B0)] public AnimationContainer Animation;

	[FieldOffset(0x21C8)] public float Opacity;
}
