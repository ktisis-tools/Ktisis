using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Posing;
using Ktisis.Posing.Bones;
using Ktisis.Data.Config.Display;
using Ktisis.Interop.Unmanaged;
using Ktisis.Common.Utility;
using Ktisis.Scene.Impl;

namespace Ktisis.Scene.Objects.Models;

public class Bone : ArmatureNode, ITransformLocal {
	// Properties

	public override ItemType ItemType => ItemType.BoneNode;

	// Constructor

	private readonly Armature Armature;

	public readonly BoneData Data;
	public uint PartialId;

	public Bone(Armature armature, BoneData bone, uint pId) {
		this.Armature = armature;

		this.Name = bone.Name;
		this.Data = bone;
		this.PartialId = pId;
	}

	// Skeleton access

	public override Armature GetArmature() => this.Armature;

	private unsafe hkaPose* GetPose(Pointer<Skeleton> skeleton) {
		if (skeleton.IsNull || skeleton.Data->PartialSkeletons == null)
			return null;

		var partial = skeleton.Data->PartialSkeletons[this.Data.PartialIndex];
		return partial.GetHavokPose(0);
	}

	private unsafe hkaPose* GetPose()
		=> GetPose(GetSkeleton());

	// ITransformLocal

	public unsafe Transform? GetLocalTransform() {
		var pose = GetPose();
		if (pose == null) return null;
		return PoseEditor.GetModelTransform(pose, this.Data.BoneIndex);
	}

	public unsafe void SetLocalTransform(Transform trans) {
		var pose = GetPose();
		if (pose == null) return;
		PoseEditor.SetModelTransform(pose, this.Data.BoneIndex, trans);
	}

	// ITransform

	public unsafe Transform? GetTransform() {
		var skeleton = GetSkeleton();
		var pose = GetPose(skeleton);
		if (pose == null) return null;
		return PoseEditor.GetWorldTransform(skeleton.Data, pose, this.Data.BoneIndex);
	}

	public unsafe void SetTransform(Transform trans) {
		var skeleton = GetSkeleton();
		var pose = GetPose(skeleton);
		if (pose == null) return;
		PoseEditor.SetWorldTransform(skeleton.Data, pose, this.Data.BoneIndex, trans);
	}

	// Helpers

	public bool MatchesId(int pId, int bId)
		=> this.Data.PartialIndex == pId && this.Data.BoneIndex == bId;
}
