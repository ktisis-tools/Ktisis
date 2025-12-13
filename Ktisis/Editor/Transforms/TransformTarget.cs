using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok.Animation.Rig;

using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Common.Utility;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Transforms.Types;

namespace Ktisis.Editor.Transforms;

public class TransformTarget : ITransformTarget {
	public SceneEntity? Primary { get; }
	public IEnumerable<SceneEntity> Targets { get; }

	public TransformSetup Setup { get; set; } = new();

	private readonly Dictionary<EntityPose, Dictionary<int, List<BoneNode>>> PoseMap;

	public TransformTarget(
		SceneEntity? primary,
		IEnumerable<SceneEntity> targets
	) {
		targets = targets.ToList();
		this.Primary = primary;
		this.Targets = targets;
		this.PoseMap = TransformResolver.BuildPoseMap(primary, targets);
	}
	
	public Transform? GetTransform() {
		if (this.Primary is ITransform transform)
			return transform.GetTransform();
		return null;
	}
	
	public void SetTransform(Transform transform) {
		var initial = this.GetTransform();
		if (initial == null) return;

		this.TransformObjects(transform, initial);
		this.TransformSkeletons(transform, initial);
	}

	private void TransformObjects(Transform transform, Transform initial) {
		Matrix4x4 deltaMx;
		if (Matrix4x4.Invert(initial.ComposeMatrix(), out var initialInverse))
			deltaMx = initialInverse * transform.ComposeMatrix();
		else return;

		switch (this.Setup.MirrorRotation) {
			case MirrorMode.Inverse:
				Matrix4x4.Invert(deltaMx, out deltaMx);
				break;
			case MirrorMode.Reflect:
				// todo: better handle reflection for separate objects not in same skeleton
				Matrix4x4.Invert(deltaMx, out deltaMx);
				break;
			case MirrorMode.Parallel:
				break;
		}

		foreach (var entity in this.Targets.Where(tar => tar is { IsValid: true } and not BoneNode)) {
			if (entity is not ITransform manip) continue;
			
			var trans = manip.GetTransform();
			if (trans == null) continue;
			
			if (entity == this.Primary) {
				manip.SetTransform(transform);
			} else {
				trans.DecomposeMatrixPrecise(trans.ComposeMatrix() * deltaMx, trans);
				manip.SetTransform(trans);
			}
		}
	}

	private unsafe void TransformSkeletons(Transform transform, Transform initial) {
		var delta = new Transform(
			transform.Position - initial.Position,
			Quaternion.Normalize(transform.Rotation / initial.Rotation),
			transform.Scale / initial.Scale
		);
		
		foreach (var (pose, partialMap) in this.PoseMap) {
			var skeleton = pose.GetSkeleton();
			if (skeleton == null || skeleton->PartialSkeletons == null)
				continue;

			var partialCt = skeleton->PartialSkeletonCount;
			for (var pIndex = 0; pIndex < partialCt; pIndex++) {
				if (!partialMap.TryGetValue(pIndex, out var boneList))
					continue;

				var partial = skeleton->PartialSkeletons[pIndex];
				var hkaPose = partial.GetHavokPose(0);
				if (hkaPose == null) continue;

				foreach (var bone in boneList.Where(bone => bone.IsValid))
					this.TransformBone(transform, initial, delta, skeleton, hkaPose, bone);
			}
		}
	}

	private unsafe void TransformBone(
		Transform transform,
		Transform initial,
		Transform delta,
		Skeleton* skeleton,
		hkaPose* hkaPose,
		BoneNode bone
	) {
		var bIndex = bone.Info.BoneIndex;
		var boneTrans = bone.GetTransform();
		if (boneTrans == null) return;

		var mirror = this.Setup.MirrorRotation is MirrorMode.Inverse or MirrorMode.Reflect;
		if (mirror && this.Primary is BoneNode pNode)
			mirror &= !bone.IsBoneDescendantOf(pNode);

		Matrix4x4 newMx;
		if (bone == this.Primary) {
			newMx = transform.ComposeMatrix();
		} else {
			var newScale = boneTrans.Scale * delta.Scale;
			Quaternion deltaRot;
			Vector3 deltaPos;

			if (mirror) {
				var localDeltaPos = Vector3.Transform(delta.Position, Quaternion.Inverse(initial.Rotation));
				if (this.Setup.MirrorRotation == MirrorMode.Inverse) {
					deltaRot = Quaternion.Conjugate(delta.Rotation);
					localDeltaPos = new Vector3(-localDeltaPos.X, -localDeltaPos.Y, -localDeltaPos.Z);
				} else {
					var localDeltaRot = Quaternion.Inverse(initial.Rotation) * delta.Rotation * initial.Rotation;
					var m = new Matrix4x4(
						-1, 0, 0, 0,
						 0, -1, 0, 0,
						 0, 0, 1, 0,
						 0, 0, 0, 1
					);
					var r = Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(localDeltaRot));
					localDeltaRot = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(m * r * m));
					deltaRot = initial.Rotation * localDeltaRot * Quaternion.Inverse(initial.Rotation);

					localDeltaPos = new Vector3(localDeltaPos.X, localDeltaPos.Y, -localDeltaPos.Z);
				}
				deltaPos = Vector3.Transform(localDeltaPos, boneTrans.Rotation);
			} else {
				deltaRot = delta.Rotation;
				deltaPos = delta.Position;
			}
			deltaRot = Quaternion.Normalize(deltaRot);

			Quaternion rotation;
			if (this.Setup.RelativeBones) {
				var linkDelta = Quaternion.Normalize(boneTrans.Rotation / initial.Rotation);
				rotation = linkDelta * deltaRot * initial.Rotation;
			} else {
				rotation = deltaRot * boneTrans.Rotation;
			}
			rotation = Quaternion.Normalize(rotation);

			var scale = Matrix4x4.CreateScale(newScale);
			var rot = Matrix4x4.CreateFromQuaternion(rotation);
			var pos = Matrix4x4.CreateTranslation(boneTrans.Position + deltaPos);
			newMx = scale * rot * pos;
		}
		
		var bInitial = HavokPosing.GetModelTransform(hkaPose, bIndex)!;
		bone.SetTransform(new Transform(newMx, bInitial));

		if (!this.Setup.ParentBones) return;

		var final = HavokPosing.GetModelTransform(hkaPose, bIndex)!;
		HavokPosing.Propagate(skeleton, bone.Info.PartialIndex, bone.Info.BoneIndex, final, bInitial);
	}
}
