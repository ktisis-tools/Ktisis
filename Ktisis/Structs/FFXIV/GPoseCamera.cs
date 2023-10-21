using System.Numerics;
using System.Runtime.InteropServices;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace Ktisis.Structs.FFXIV {
	[StructLayout(LayoutKind.Explicit)]
	public struct GPoseCamera {
		[FieldOffset(0x60)] public Vector3 Position;

		[FieldOffset(0x114)] public float Distance;
		[FieldOffset(0x118)] public float DistanceMin;
		[FieldOffset(0x11C)] public float DistanceMax;
		[FieldOffset(0x12C)] public float FoV;
		[FieldOffset(0x130)] public Vector2 Angle;
		[FieldOffset(0x150)] public Vector2 Pan;
		[FieldOffset(0x160)] public float Rotation;
		[FieldOffset(0x208)] public Vector2 DistanceCollide;
		[FieldOffset(0x14C)] public float YMin;
		[FieldOffset(0x148)] public float YMax;

		public Vector3 CalcRotation() => new(Angle.X - Pan.X, -Angle.Y - Pan.Y, Rotation);
	}
}