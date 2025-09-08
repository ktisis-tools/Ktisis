using System.Runtime.InteropServices;

using RenderCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera;

namespace Ktisis.Structs.Camera;

[StructLayout(LayoutKind.Explicit)]
public struct RenderCameraEx {
	[FieldOffset(0x00)] public RenderCamera RenderCamera;
	
	[FieldOffset(0x1EC)] public float FoV;
	[FieldOffset(0x1F0)] public float AspectRatio;

	[FieldOffset(0x1FC)] public float OrthographicZoom;
	[FieldOffset(0x200)] public bool OrthographicEnabled;
}
