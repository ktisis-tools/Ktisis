using System.Numerics;

using FFXIVClientStructs.Havok;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using Ktisis.Posing;
using Ktisis.Posing.Bones;
using Ktisis.Scene.Impl;
using Ktisis.Common.Utility;
using Ktisis.Data.Config.Display;
using Ktisis.Interop.Unmanaged;

namespace Ktisis.Scene.Objects.Models; 

public class Bone : ArmatureNode, IManipulable {
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
	
	// Armature access

	public override Armature GetArmature() => this.Armature;
	
	// Helpers

	public bool MatchesId(int pId, int bId)
		=> this.Data.PartialIndex == pId && this.Data.BoneIndex == bId;
	
	// IManipulable

    private unsafe hkaPose* GetPose(Pointer<Skeleton> skeleton) {
		if (skeleton.IsNullPointer || skeleton.Data->PartialSkeletons == null)
			return null;

		var partial = skeleton.Data->PartialSkeletons[this.Data.PartialIndex];
		return partial.GetHavokPose(0);
	}

	public unsafe Transform? GetTransform() {
		var skeleton = GetSkeleton();
		var pose = GetPose(skeleton);
		if (pose == null) return null;

		return PoseEditor.GetWorldTransform(skeleton.Data, pose, this.Data.BoneIndex);
	}

	public unsafe void SetTransform(Transform trans) {
		var skeleton = this.GetSkeleton();
		var pose = GetPose(skeleton);
		if (pose == null) return;

		var initial = PoseEditor.GetModelTransform(pose, this.Data.BoneIndex);
		if (initial is null) return;

		var skeleTrans = new Transform(skeleton.Data->Transform);
		var modelTrans = PoseEditor.WorldToModel(trans, skeleTrans);
		PoseEditor.SetModelTransform(pose, this.Data.BoneIndex, modelTrans);
	}
}
