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
		this.Position = Vector3.Zero;
		this.Rotation = Quaternion.Identity;
		this.Scale = Vector3.One;
	}

	public Transform(Vector3 pos, Quaternion rot, Vector3 scale) {
		this.Position = pos;
		this.Rotation = rot;
		this.Scale = scale;
	}

	public Transform(hkQsTransformf hk) {
		this.Position = hk.Translation.ToVector3();
		this.Rotation = hk.Rotation.ToQuaternion();
		this.Scale = hk.Scale.ToVector3();
	}

	public Transform(Matrix4x4 mx) {
		DecomposeMatrix(mx);
	}
	
	// Havok

	public void ApplyTo(ref hkQsTransformf hk) {
		hk.Translation.SetFrom(this.Position);
		hk.Rotation.SetFrom(this.Rotation);
		hk.Scale.SetFrom(this.Scale);
	}
	
	// Matrix

	public Matrix4x4 ComposeMatrix() {
		var sclMx = Matrix4x4.CreateScale(this.Scale);
		var rotMx = Matrix4x4.CreateFromQuaternion(this.Rotation);
		var posMx = Matrix4x4.CreateTranslation(this.Position);
		return sclMx * rotMx * posMx;
	}

	public void DecomposeMatrix(Matrix4x4 mx) {
		if (!Matrix4x4.Decompose(
			mx,
			out var scl,
			out var rot,
			out var pos
		)) return;
		
		this.Position = pos;
		this.Rotation = rot;
		this.Scale = scl;
	}
}