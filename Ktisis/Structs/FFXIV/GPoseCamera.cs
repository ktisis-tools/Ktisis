using System.Numerics;
using System.Runtime.InteropServices;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace Ktisis.Structs.FFXIV {
	[StructLayout(LayoutKind.Explicit)]
	public struct GPoseCamera {
		[FieldOffset(0)] public GameCamera GameCamera;

		[FieldOffset(0x010)] public Vector3 Position;

		[FieldOffset(0x114)] public float Zoom;
		[FieldOffset(0x12C)] public float FoV;
		[FieldOffset(0x130)] public Vector2 Angle;
		[FieldOffset(0x150)] public Vector2 Pan;
		[FieldOffset(0x160)] public float Rotation;
	}
}