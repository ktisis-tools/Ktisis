using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actors;

[StructLayout(LayoutKind.Explicit, Size = 0x1BD0)]
public struct CharacterEx {
	[FieldOffset(0x970)] public AnimationContainer Animation;
}
