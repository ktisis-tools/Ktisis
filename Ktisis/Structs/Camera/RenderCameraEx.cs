using System.Runtime.InteropServices;

using RenderCamera = FFXIVClientStructs.FFXIV.Client.Graphics.Render.Camera;

namespace Ktisis.Structs.Camera;

[StructLayout(LayoutKind.Explicit)]
public struct RenderCameraEx {
	[FieldOffset(0x00)] public RenderCamera RenderCamera;
	
	[FieldOffset(0xA8)] public float FoV;
	[FieldOffset(0xAC)] public float AspectRatio;

	[FieldOffset(0xB8)] public float OrthographicZoom;
	[FieldOffset(0xBC)] public bool OrthographicEnabled;
}
