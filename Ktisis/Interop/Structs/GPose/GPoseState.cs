using System;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Interop;

using Ktisis.Interop.Structs.Lights;

namespace Ktisis.Interop.Structs.GPose;

[StructLayout(LayoutKind.Explicit)]
public struct GPoseState {
	private const int LightCount = 3;
	
	[FieldOffset(0xE0)] public unsafe fixed ulong Lights[LightCount];
	
	// Light access
	
	public unsafe SceneLight* GetLight(uint index) => (SceneLight*)this.Lights[index];

	public unsafe Span<Pointer<SceneLight>> GetLights() {
		fixed (ulong* ptr = this.Lights) {
			return new Span<Pointer<SceneLight>>(ptr, LightCount);
		}
	}
}
