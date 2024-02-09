using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Common.Utility;
using Ktisis.Editor.Posing;
using Ktisis.Editor.Transforms.Types;

namespace Ktisis.Editor.Transforms;

public class TransformTarget : ITransformTarget {
	private readonly TransformHandler _handler;
	
	public SceneEntity? Primary { get; }
	public IEnumerable<SceneEntity> Targets { get; }

	private readonly Dictionary<EntityPose, Dictionary<int, List<BoneNode>>> PoseMap;

	public TransformTarget(
		TransformHandler handler,
		SceneEntity? primary,
		IEnumerable<SceneEntity> targets
	) {
		this._handler = handler;
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

		if (this._handler.IsMirrored)
			Matrix4x4.Invert(deltaMx, out deltaMx);

		foreach (var entity in this.Targets.Where(tar => tar is { IsValid: true } and not BoneNode)) {
			if (entity is not ITransform manip) continue;
			
			var trans = manip.GetTransform();
			if (trans == null) continue;
			
			if (entity == this.Primary) {
				manip.SetTransform(transform);
			} else {
				trans.DecomposeMatrix(trans.ComposeMatrix() * deltaMx);
				manip.SetTransform(trans);
			}
		}
	}

	private unsafe void TransformSkeletons(Transform transform, Transform initial) {
		var delta = new Transform(
			transform.Position - initial.Position,
			transform.Rotation / initial.Rotation,
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
					this.TransformBone(transform, delta, skeleton, hkaPose, bone);
			}
		}
	}

	private unsafe void TransformBone(Transform transform, Transform delta, Skeleton* skeleton, hkaPose* hkaPose, BoneNode bone) {
		var bIndex = bone.Info.BoneIndex;
		var boneTrans = bone.GetTransform();
		if (boneTrans == null) return;

		var mirror = this._handler.IsMirrored;
		if (mirror && this.Primary is BoneNodeGroup group)
			mirror &= !bone.IsChildOf(group);

		Matrix4x4 newMx;
		if (bone == this.Primary) {
			newMx = transform.ComposeMatrix();
		} else {
			var newScale = boneTrans.Scale * delta.Scale;
			Quaternion deltaRot;
			Vector3 deltaPos;
			
			if (mirror) {
				deltaRot = Quaternion.Inverse(delta.Rotation);
				deltaPos = -delta.Position;
			} else {
				deltaRot = delta.Rotation;
				deltaPos = delta.Position;
			}

			var scale = Matrix4x4.CreateScale(newScale);
			var rot = Matrix4x4.CreateFromQuaternion(deltaRot * boneTrans.Rotation);
			var pos = Matrix4x4.CreateTranslation(boneTrans.Position + deltaPos);
			newMx = scale * rot * pos;
		}
		
		var initial = HavokPosing.GetModelTransform(hkaPose, bIndex)!;
		bone.SetMatrix(newMx);

		var final = HavokPosing.GetModelTransform(hkaPose, bIndex)!;
		HavokPosing.Propagate(skeleton, bone.Info.PartialIndex, bone.Info.BoneIndex, final, initial);
	}
}
