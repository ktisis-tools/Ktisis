using System;
using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using CSTransform = FFXIVClientStructs.FFXIV.Client.Graphics.Transform;

using Ktisis.Common.Extensions;

namespace Ktisis.Common.Utility;

[StructLayout(LayoutKind.Explicit)]
public class Transform : IEquatable<Transform> {
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

	public Transform(CSTransform trans) {
		this.Position = trans.Position;
		this.Rotation = trans.Rotation;
		this.Scale = trans.Scale;
	}

	public Transform(Matrix4x4 mx) {
		this.DecomposeMatrix(mx);
	}

	public Transform(Matrix4x4 mx, Transform initial) {
		this.DecomposeMatrixPrecise(mx, initial);
	}

	public Transform(Vector3 pos) {
		this.Position = pos;
		this.Rotation = Quaternion.Identity;
		this.Scale = Vector3.One;
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

	public void DecomposeMatrixPrecise(Matrix4x4 mx, Transform initial) {
		const float posEps = 1e-4f;
		const float rotAngleEps = 1e-4f;
		const float scaRelEps = 1e-4f;

		var aPos = initial.Position;
		var aRot = initial.Rotation;
		var aSca = initial.Scale;

		Matrix4x4.Decompose(mx, out var bSca, out var bRot, out var bPos);

		this.Position = (bPos - aPos).LengthSquared() < posEps * posEps ? aPos : bPos;

		if (Quaternion.Dot(bRot, aRot) < 0f) {
			bRot = new Quaternion(-bRot.X, -bRot.Y, -bRot.Z, -bRot.W);
		}
		var dot = Math.Clamp(Quaternion.Dot(bRot, aRot), -1f, 1f);
		var angle = 2f * MathF.Acos(dot);
		this.Rotation = angle < rotAngleEps ? aRot : bRot;

		Vector3 resSca = bSca;
		resSca.X = IsScaleJitter(aSca.X, bSca.X, scaRelEps) ? aSca.X : bSca.X;
		resSca.Y = IsScaleJitter(aSca.Y, bSca.Y, scaRelEps) ? aSca.Y : bSca.Y;
		resSca.Z = IsScaleJitter(aSca.Z, bSca.Z, scaRelEps) ? aSca.Z : bSca.Z;
		this.Scale = resSca;
	}

	private static bool IsScaleJitter(float a, float b, float relEps) {
		const float minMag = 1e-6f;
		var mag = MathF.Max(MathF.Abs(a), minMag);
		var rel = MathF.Abs(b - a) / mag;
		return rel < relEps;
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

	public bool Equals(Transform? trans) =>
		trans != null
		&& this.Position.Equals(trans.Position)
		&& this.Rotation.Equals(trans.Rotation)
		&& this.Scale.Equals(trans.Scale);
}
