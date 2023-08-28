using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Ktisis.Interop.Structs.Objects;
using Ktisis.Interop.Unmanaged;

namespace Ktisis.Interop.Structs.Event; 

[StructLayout(LayoutKind.Explicit)]
public struct GPoseModule {
	public const int LightCount = 3;
	
	[FieldOffset(0xE0)] public unsafe fixed ulong Lights[LightCount];

	public unsafe Pointer<Light> GetLight(int index) {
		if (index is < 0 or > LightCount)
			throw new ArgumentOutOfRangeException(nameof(index));
		return new Pointer<Light>((Light*)this.Lights[index]);
	}

	public IEnumerable<Pointer<Light>> GetLights() {
		for (var i = 0; i < LightCount; i++)
			yield return GetLight(i);
	}
}