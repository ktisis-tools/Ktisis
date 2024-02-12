using System.Runtime.InteropServices;

namespace Ktisis.Structs.Actors;

[StructLayout(LayoutKind.Explicit, Size = 0x1BD0)]
public struct CharacterEx {
	[FieldOffset(0x0970)] public AnimationContainer Animation;

	[FieldOffset(0x1B2C)] public float Opacity;
}
