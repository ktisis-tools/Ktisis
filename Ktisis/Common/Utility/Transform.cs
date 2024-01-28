using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Havok;
using CSTransform = FFXIVClientStructs.FFXIV.Client.Graphics.Transform;

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
		Ktisis.Log.Info($"{hk.Translation.X} {hk.Translation.Y} {hk.Translation.Z} {hk.Translation.W}");
		this.Position = hk.Translation.ToVector3();
		this.Rotation = hk.Rotation.ToQuaternion();
		this.Scale = hk.Scale.ToVector3();
	}

	public Transform(CSTransform trans) {
		this.Position = trans.Position;
		this.Rotation = trans.Rotation;
		this.Scale = trans.Scale;
	}

	public Transform(Matrix4x4 mx) {
		this.DecomposeMatrix(mx);
	}
	
	// Matrix

	public Matrix4x4 ComposeMatrix() {
		var sclMx = Matrix4x4.CreateScale(this.Scale);
		var rotMx = Matrix4x4.CreateFromQuaternion(this.Rotation);
		var posMx = Matrix4x4.CreateTranslation(this.Position);
		return sclMx * rotMx * posMx;
	}

	public void DecomposeMatrix(Matrix4x4 mx) {
		// TODO: Look into why this returns false when an actor is scaled, lol.
		Matrix4x4.Decompose(
			mx,
			out var scl,
			out var rot,
			out var pos
		);
		
		this.Position = pos;
		this.Rotation = rot;
		this.Scale = scl;
	}
	
	// Set

	public Transform Set(Transform t) {
		this.Position = t.Position;
		this.Rotation = t.Rotation;
		this.Scale = t.Scale;
		return this;
	}
	
	// ClientStructs conversion

	public static implicit operator CSTransform(Transform trans) => new() {
		Position = trans.Position,
		Rotation = trans.Rotation,
		Scale = trans.Scale
	};
}
