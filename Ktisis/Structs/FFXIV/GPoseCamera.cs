using System.Numerics;
using System.Runtime.InteropServices;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace Ktisis.Structs.FFXIV {
	[StructLayout(LayoutKind.Explicit)]
	public struct GPoseCamera {
		[FieldOffset(0x60)] public Vector3 Position;

		[FieldOffset(0x124)] public float Distance;
		[FieldOffset(0x128)] public float DistanceMin;
		[FieldOffset(0x12C)] public float DistanceMax;
		[FieldOffset(0x13C)] public float FoV;
		[FieldOffset(0x140)] public Vector2 Angle;
		[FieldOffset(0x15C)] public float YMin;
		[FieldOffset(0x158)] public float YMax;
		[FieldOffset(0x160)] public Vector2 Pan;
		[FieldOffset(0x170)] public float Rotation;
		[FieldOffset(0x218)] public Vector2 DistanceCollide;

		public Vector3 CalcRotation() => new(Angle.X - Pan.X, -Angle.Y - Pan.Y, Rotation);
	}
}