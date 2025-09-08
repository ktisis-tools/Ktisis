using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Ktisis.Common.Utility;

using GameCamera = FFXIVClientStructs.FFXIV.Client.Game.Camera;

namespace Ktisis.Structs.Camera;

[StructLayout(LayoutKind.Explicit)]
public struct GameCameraEx {
	[FieldOffset(0x000)] public GameCamera GameCamera;
	
	[FieldOffset(0x060)] public Vector3 Position;

	[FieldOffset(0x124)] public float Distance;
	[FieldOffset(0x128)] public float DistanceMin;
	[FieldOffset(0x12C)] public float DistanceMax;
	[FieldOffset(0x13C)] public float Zoom;
	[FieldOffset(0x140)] public Vector2 Angle;
	[FieldOffset(0x15C)] public float YMin;
	[FieldOffset(0x158)] public float YMax;
	[FieldOffset(0x160)] public Vector2 Pan;
	[FieldOffset(0x170)] public float Rotation;
	[FieldOffset(0x218)] public Vector2 DistanceCollide;

	public Quaternion CalcPointDirection() {
		return (new Vector3(
			-(this.Angle.Y + this.Pan.Y),
			(this.Angle.X + 3.14159f) % 6.28319f - this.Pan.X,
			0.0f
		) * MathHelpers.Rad2Deg).EulerAnglesToQuaternion();
	}
	
	public Vector3 CalcRotation() => new(
		this.Angle.X - this.Pan.X,
		-this.Angle.Y - this.Pan.Y,
		this.Rotation
	);

	public unsafe RenderCameraEx* RenderEx
		=> (RenderCameraEx*)this.GameCamera.CameraBase.SceneCamera.RenderCamera;

	public unsafe static GameCameraEx* GetActive()
		=> (GameCameraEx*)CameraManager.Instance()->GetActiveCamera();
}
