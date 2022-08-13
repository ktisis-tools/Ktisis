using System;
using System.Numerics;

using ImGuizmoNET;

using Ktisis.Helpers;
using Ktisis.Structs.Actor;

namespace Ktisis.Structs.Bones {
	public class BoneMod {
		public SharpDX.Matrix BoneMatrix;
		public SharpDX.Matrix DeltaMatrix;

		public Vector3 WorldPos;
		public Vector3 Rotation;
		public Vector3 Scale;

		public Quaternion RootRotation;
		public float ScaleModifier;

		public BoneMod() {
			BoneMatrix = new SharpDX.Matrix();
			DeltaMatrix = new SharpDX.Matrix();

			WorldPos = new Vector3();
			Rotation = new Vector3();
			Scale = new Vector3(1.0f, 1.0f, 1.0f);

			RootRotation = new Quaternion();
			ScaleModifier = 1.0f;
		}

		public unsafe void SnapshotBone(Bone bone, ActorModel* model) {
			RootRotation = model->Rotation;
			ScaleModifier = model->Height;

			WorldPos = model->Position + bone.Rotate(RootRotation) * ScaleModifier;

			Rotation = MathHelpers.ToEuler(bone.Transform.Rotate * RootRotation);
			Scale = MathHelpers.ToVector3(bone.Transform.Scale);

			ImGuizmo.RecomposeMatrixFromComponents(
				ref WorldPos.X,
				ref Rotation.X,
				ref Scale.X,
				ref BoneMatrix.M11
			);
		}

		public Transform GetDelta() {
			// Create vectors

			var translate = new Vector3();
			var rotation = new Vector3();
			var scale = new Vector3();

			// Decompose into vectors
			/*
				This is a little hacky due to 2 separate bugs -
				Rotation via BoneMatrix suffers from gimbal lock
				Rotation via DeltaMatrix also results in unwanted translation
			*/

			var _ = new Vector3();

			ImGuizmo.DecomposeMatrixToComponents(
				ref BoneMatrix.M11,
				ref translate.X,
				ref _.X,
				ref _.X
			);

			ImGuizmo.DecomposeMatrixToComponents(
				ref DeltaMatrix.M11,
				ref _.X,
				ref rotation.X,
				ref scale.X
			);

			// Convert to Transform

			var delta = new Transform();

			// Convert position

			var inverse = Quaternion.Inverse(RootRotation);
			delta.Translate = Vector4.Transform(
				translate - WorldPos,
				inverse
			) / ScaleModifier;

			// Attempt rotation

			delta.Rotate = MathHelpers.ToQuaternion(rotation);

			// Update stored values

			WorldPos = translate;
			Rotation = rotation;
			Scale = scale;

			// :D

			return delta;
		}
	}
}