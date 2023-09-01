using System.Numerics;
using System.Collections.Generic;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Common.Utility;
using Ktisis.Posing.Bones;
using Ktisis.Interop.Unmanaged;

namespace Ktisis.Posing;

public class PoseEditor {
	// Constructor

	private readonly Pointer<Skeleton> Skeleton;
	private readonly Pointer<hkaPose> Pose = new();

	private int? BoneIndex;
	private int? PartialIndex;

	public PoseEditor(Pointer<Skeleton> skeleton) {
		this.Skeleton = skeleton;
	}

	// Bone access

	public PoseEditor SetBone(BoneData bone) {
		SetBone(bone.BoneIndex, bone.PartialIndex);
		return this;
	}

	private void SetBone(int boneX, int partX) {
		if (this.PartialIndex != partX)
			SetPartial(partX);
		this.BoneIndex = boneX;
	}

	// PartialSkeleton & hkaPose

	private unsafe void SetPartial(int index) {
		var partial = GetPartial(index);
		this.Pose.Data = partial is null ? null : partial.Value.GetHavokPose(0);
		this.PartialIndex = index;
	}

	private unsafe PartialSkeleton? GetPartial(int index) {
		if (this.Skeleton.IsNull)
			return null;

		var partials = this.Skeleton.Data->PartialSkeletons;
		if (partials == null || partials[index].HavokPoses == null)
			return null;
		return partials[index];
	}

	// Bone transform

	private unsafe hkQsTransformf* AccessModelSpace(int index)
		=> this.Pose.Data->AccessBoneModelSpace(index, hkaPose.PropagateOrNot.DontPropagate);

	public unsafe Transform? GetTransform() {
		if (this.BoneIndex is null || this.Pose.IsNull)
			return null;

		var access = AccessModelSpace(this.BoneIndex.Value);
		return access == null ? null : new Transform(*access);
	}

	public unsafe Transform? GetWorldTransform() {
		var trans = GetTransform();
		if (trans is null)
			return null;

		var skele = new Transform(this.Skeleton.Data->Transform);
		var matrix = trans.ComposeMatrix() * skele.ComposeMatrix();
		trans.DecomposeMatrix(matrix);
		return trans;
	}

	public unsafe void SetWorldTransform(Transform trans) {
		if (this.BoneIndex is null)
			return;

		var skeleTrans = new Transform(this.Skeleton.Data->Transform);
		Matrix4x4.Invert(skeleTrans.ComposeMatrix(), out var invert);

		var matrix = trans.ComposeMatrix() * invert;
		trans.DecomposeMatrix(matrix);

		var access = AccessModelSpace(this.BoneIndex.Value);
		if (access != null)
			*access = trans.ToHavok();
	}

	public void Propagate(Transform target, Transform initial)
		=> Propagate(this.Pose,	 target, initial);

	private unsafe void Propagate(Pointer<hkaPose> pose, Transform target, Transform initial) {
		if (this.BoneIndex is null)
			return;

		var hkaSkeleton = pose.Data->Skeleton;
		if (hkaSkeleton == null)
			return;

		var deltaPos = target.Position - initial.Position;
		var deltaRot = target.Rotation / initial.Rotation;

		var bones = Recurse(this.BoneIndex.Value);
		if (this.PartialIndex != 0)
			return;

		for (var p = 1; p < this.Skeleton.Data->PartialSkeletonCount; p++) {
			var partial = GetPartial(p);
			if (partial is null || !bones.Contains(partial.Value.ConnectedParentBoneIndex))
				continue;

			var init = this.BoneIndex.Value;
			try {
				SetBone(partial.Value.ConnectedBoneIndex, p);
				Propagate(this.Pose, target, initial);
			} finally {
				SetBone(init, 0);
			}
		}

		return;

		List<int> Recurse(int idx, List<int>? desc = null) {
			desc ??= new List<int>();

			for (var i = 1; i < hkaSkeleton->Bones.Length; i++) {
				var pIdx = hkaSkeleton->ParentIndices[i];
				if (pIdx != idx) continue;

				var access = AccessModelSpace(i);
				var trans = new Transform(*access);

				var offset = Vector3.Transform(trans.Position - target.Position, deltaRot);
				var matrix = trans.ComposeMatrix() * Matrix4x4.CreateFromQuaternion(deltaRot);
				matrix.Translation = target.Position + deltaPos + offset;
				trans.DecomposeMatrix(matrix);
				*access = trans.ToHavok();

				desc.Add(i);
				Recurse(i, desc);
			}

			return desc;
		}
	}
}
