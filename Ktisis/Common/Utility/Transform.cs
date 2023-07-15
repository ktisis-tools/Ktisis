using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Havok;

using Ktisis.Common.Extensions;

namespace Ktisis.Common.Utility;

[StructLayout(LayoutKind.Explicit)]
public class Transform {
	[FieldOffset(0x00)] public Vector3 Position;
	[FieldOffset(0x10)] public Quaternion Rotation;
	[FieldOffset(0x20)] public Vector3 Scale;

	// Constructors

	public Transform() {
		Position = Vector3.Zero;
		Rotation = Quaternion.Identity;
		Scale = Vector3.One;
	}

	public Transform(Vector3 pos, Quaternion rot, Vector3 scale) {
		Position = pos;
		Rotation = rot;
		Scale = scale;
	}

	public Transform(hkQsTransformf hk) {
		Position = hk.Translation.ToVector3();
		Rotation = hk.Rotation.ToQuaternion();
		Scale = hk.Scale.ToVector3();
	}

	public Transform(Matrix4x4 mx) {
		DecomposeMatrix(mx);
	}

	// Havok

	public void ApplyTo(ref hkQsTransformf hk) {
		hk.Translation.SetFrom(Position);
		hk.Rotation.SetFrom(Rotation);
		hk.Scale.SetFrom(Scale);
	}

	// Matrix

	public Matrix4x4 ComposeMatrix() {
		var sclMx = Matrix4x4.CreateScale(Scale);
		var rotMx = Matrix4x4.CreateFromQuaternion(Rotation);
		var posMx = Matrix4x4.CreateTranslation(Position);
		return sclMx * rotMx * posMx;
	}

	public void DecomposeMatrix(Matrix4x4 mx) {
		if (!Matrix4x4.Decompose(
			mx,
			out var scl,
			out var rot,
			out var pos
		)) return;

		Position = pos;
		Rotation = rot;
		Scale = scl;
	}
}
