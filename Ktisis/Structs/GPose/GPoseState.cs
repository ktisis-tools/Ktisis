using System;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.Interop;

using Ktisis.Structs.Lights;

namespace Ktisis.Structs.GPose;

[StructLayout(LayoutKind.Explicit)]
public struct GPoseState {
	private const int LightCount = 3;
	
	[FieldOffset(0x0E0)] public unsafe fixed ulong Lights[LightCount];
	
	[FieldOffset(0x1E0)] public unsafe GameObject* PrimaryActor;
	
	// Light access
	
	public unsafe SceneLight* GetLight(uint index) => (SceneLight*)this.Lights[index];

	public unsafe Span<Pointer<SceneLight>> GetLights() {
		fixed (ulong* ptr = this.Lights) {
			return new Span<Pointer<SceneLight>>(ptr, LightCount);
		}
	}
}
