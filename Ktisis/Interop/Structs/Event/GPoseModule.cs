using System;
using System.Runtime.InteropServices;

using Ktisis.Interop.Structs.Objects;

namespace Ktisis.Interop.Structs.Event;

[StructLayout(LayoutKind.Explicit)]
public struct GPoseModule {
	// Struct definition

	public const int LightCount = 3;

	[FieldOffset(0xE0)] public unsafe fixed ulong Lights[LightCount];

	// Helpers

	public unsafe Light* GetLight(int index) {
		if (index is < 0 or > LightCount)
			throw new ArgumentOutOfRangeException(nameof(index));
		return (Light*)Lights[index];
	}
}
