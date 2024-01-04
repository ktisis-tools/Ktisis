using System;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Interop;

using Ktisis.Interop.Structs.Scene;

namespace Ktisis.Interop.Structs.Game;

[StructLayout(LayoutKind.Explicit)]
public struct GPoseState {
	private const int LightCount = 3;
	
	[FieldOffset(0xE0)] public unsafe fixed ulong Lights[LightCount];
	
	// Light access
	
	public unsafe Light* GetLight(uint index) => (Light*)this.Lights[index];

	public unsafe Span<Pointer<Light>> GetLights() {
		fixed (ulong* ptr = this.Lights) {
			return new Span<Pointer<Light>>(ptr, LightCount);
		}
	}
}
