using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

using Ktisis.Editor.Posing;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Transforms;

public interface ITransformTarget : ITransform {
	public SceneEntity? Primary { get; }
	public IEnumerable<SceneEntity> Targets { get; }
}

public class TransformTarget : ITransformTarget {
	public SceneEntity? Primary { get; }
	public IEnumerable<SceneEntity> Targets { get; }

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
	
	public Common.Utility.Transform? GetTransform() {
		if (this.Primary is ITransform transform)
			return transform.GetTransform();
		return null;
	}
	
	public void SetTransform(Common.Utility.Transform transform) {
		var initial = this.GetTransform();
		if (initial == null) return;

		this.TransformObjects(transform, initial);
		this.TransformSkeletons(transform, initial);
	}

	private void TransformObjects(Common.Utility.Transform transform, Common.Utility.Transform initial) {
		Matrix4x4 deltaMx;
		if (Matrix4x4.Invert(initial.ComposeMatrix(), out var initialInverse))
			deltaMx = initialInverse * transform.ComposeMatrix();
		else return;

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

	private unsafe void TransformSkeletons(Common.Utility.Transform transform, Common.Utility.Transform initial) {
		var delta = new Common.Utility.Transform(
			transform.Position - initial.Position,
			Quaternion.Normalize(transform.Rotation * Quaternion.Inverse(initial.Rotation)),
			transform.Scale / initial.Scale
		);
		
		foreach (var (pose, partialMap) in this.PoseMap) {
			var skeleton = pose.GetSkeleton();
			if (skeleton == null || skeleton->PartialSkeletons == null)
				continue;

			var model = new Common.Utility.Transform(skeleton->Transform);

			var partialCt = skeleton->PartialSkeletonCount;
			for (var pIndex = 0; pIndex < partialCt; pIndex++) {
				if (!partialMap.TryGetValue(pIndex, out var boneList))
					continue;

				var partial = skeleton->PartialSkeletons[pIndex];
				var hkaPose = partial.GetHavokPose(0);
				if (hkaPose == null) continue;

				foreach (var bone in boneList.Where(bone => bone.IsValid))
					this.TransformBone(transform, delta, model, skeleton, hkaPose, bone);
			}
		}
	}

	private unsafe void TransformBone(Common.Utility.Transform transform, Common.Utility.Transform delta, Common.Utility.Transform model, Skeleton* skeleton, hkaPose* hkaPose, BoneNode bone) {
		var bIndex = bone.Info.BoneIndex;
		var boneTrans = HavokPoseUtil.GetWorldTransform(skeleton, hkaPose, bIndex);
		if (boneTrans == null) return;

		Matrix4x4 newMx;
		if (bone == this.Primary) {
			newMx = transform.ComposeMatrix();
		} else {
			var newScale = boneTrans.Scale * delta.Scale;
			var deltaRot = delta.Rotation;
			var deltaPos = delta.Position;

			var scale = Matrix4x4.CreateScale(newScale);
			var rot = Matrix4x4.CreateFromQuaternion(deltaRot * boneTrans.Rotation);
			var pos = Matrix4x4.CreateTranslation(boneTrans.Position + deltaPos);
			newMx = scale * rot * pos;
		}

		HavokPoseUtil.SetWorldTransform(skeleton, hkaPose, bIndex, newMx);
		this.PropagateBones(model, boneTrans, skeleton, hkaPose, bone);
	}

	private unsafe void PropagateBones(Common.Utility.Transform model, Common.Utility.Transform boneTrans, Skeleton* skeleton, hkaPose* hkaPose, BoneNode bone) {
		var initialModel = boneTrans.WorldToModel(model);
		var finalModel = HavokPoseUtil.GetModelTransform(hkaPose, bone.Info.BoneIndex);
		if (finalModel != null)
			HavokPoseUtil.Propagate(skeleton, bone.Info.PartialIndex, bone.Info.BoneIndex, finalModel, initialModel);
	}
}
