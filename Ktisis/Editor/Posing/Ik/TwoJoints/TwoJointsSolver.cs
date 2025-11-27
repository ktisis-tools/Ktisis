using System;
using System.Numerics;

using FFXIVClientStructs.Havok.Animation.Rig;

using Ktisis.Common.Utility;
using Ktisis.Interop;
using Ktisis.Structs.Havok;

namespace Ktisis.Editor.Posing.Ik.TwoJoints;

public class TwoJointsSolver(IkModule module) : IDisposable {
	private readonly Alloc<TwoJointsIkSetup> AllocIkSetup = new(16);
	
	public unsafe TwoJointsIkSetup* IkSetup => this.AllocIkSetup.Data;
	private Transform? LastPoseInModel = null;
	
	// Setup parameters

	public unsafe void Setup() {
		if (this.AllocIkSetup.Address == nint.Zero)
			throw new Exception("Allocation for IkSetup failed.");
		
		*this.IkSetup = new TwoJointsIkSetup {
			m_firstJointIdx = -1,
			m_secondJointIdx = -1,
			m_endBoneIdx = -1,
			m_firstJointTwistIdx = -1,
			m_secondJointTwistIdx = -1,
			m_hingeAxisLS = new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
			m_cosineMaxHingeAngle = -1.0f,
			m_cosineMinHingeAngle = 1.0f,
			m_firstJointIkGain = 1.0f,
			m_secondJointIkGain = 1.0f,
			m_endJointIkGain = 1.0f,
			m_endTargetMS = Vector4.Zero,
			m_endTargetRotationMS = Quaternion.Identity,
			m_endBoneOffsetLS = Vector4.Zero,
			m_endBoneRotationOffsetLS = Quaternion.Identity,
			m_enforceEndPosition = true,
			m_enforceEndRotation = false
		};
	}
	
	// Solving

	public unsafe bool Solve(hkaPose* poseIn, hkaPose* poseOut, bool frozen = false) {
		if (poseOut == null || poseOut->Skeleton == null)
			return false;
		
		if (frozen) {
			poseIn->SetToReferencePose();
			poseIn->SyncModelSpace();
			this.UpdateModelPose(poseIn, poseOut);
		}
		
		byte result = 0;
		module.SolveTwoJoints(&result, this.IkSetup, poseIn);
		
		if (result == 0) return false;

		poseIn->SyncModelSpace();
		if (frozen)
			this.ApplyModelPoseStatic(poseIn, poseOut);
		else
			this.ApplyModelPoseDynamic(poseIn, poseOut);

		return true;
	}
	
	public unsafe bool SolveGroup(hkaPose* poseIn, hkaPose* poseOut, TwoJointsGroup group, bool frozen = false) {
		if (!group.IsEnabled) return false;
		
		var ik = this.IkSetup;
		
		ik->m_firstJointIdx = group.FirstBoneIndex;
		ik->m_firstJointTwistIdx = group.FirstTwistIndex;
		ik->m_secondJointIdx = group.SecondBoneIndex;
		ik->m_secondJointTwistIdx = group.SecondTwistIndex;
		ik->m_endBoneIdx = group.EndBoneIndex;

		ik->m_firstJointIkGain = group.FirstBoneGain;
		ik->m_secondJointIkGain = group.SecondBoneGain;
		ik->m_endJointIkGain = group.EndBoneGain;

		ik->m_enforceEndPosition = group.EnforcePosition;
		ik->m_enforceEndRotation = group.EnforceRotation;

		ik->m_hingeAxisLS = new Vector4(group.HingeAxis, 1.0f);
		ik->m_cosineMinHingeAngle = group.MinHingeAngle;
		ik->m_cosineMaxHingeAngle = group.MaxHingeAngle;
		
		var target = HavokPosing.GetModelTransform(poseOut, group.EndBoneIndex);
		if (target == null) return false;

		var isRelative = group.Mode == TwoJointsMode.Relative;
		if (isRelative || !group.EnforcePosition)
			group.TargetPosition = target.Position;
		if (isRelative || !group.EnforceRotation)
			group.TargetRotation = target.Rotation;
		
		ik->m_endTargetMS = new Vector4(group.TargetPosition, 0.0f);
		ik->m_endTargetRotationMS = group.TargetRotation;

		return this.Solve(poseIn, poseOut, frozen);
	}

	private unsafe void UpdateModelPose(hkaPose* poseIn, hkaPose* poseOut) {
		var start = this.IkSetup->m_firstJointIdx;
		for (var i = 1; i < poseIn->Skeleton->Bones.Length; i++) {
			if (i != start && !HavokPosing.IsBoneDescendantOf(poseOut->Skeleton->ParentIndices, start, i))
				continue;
			*poseIn->AccessBoneModelSpace(i, hkaPose.PropagateOrNot.Propagate) = poseOut->ModelPose[i];
		}
	}

	private unsafe void ApplyModelPoseStatic(hkaPose* poseIn, hkaPose* poseOut) {
		var parents = poseOut->Skeleton->ParentIndices;
		hkaSkeletonUtils.transformModelPoseToLocalPose(
			poseOut->Skeleton->Bones.Length,
			parents.Data,
			poseOut->ModelPose.Data,
			poseIn->LocalPose.Data
		);
		
		var start = this.IkSetup->m_firstJointIdx;
		var end = this.IkSetup->m_endBoneIdx;
		var poseInModel = HavokPosing.GetModelTransform(poseIn, end)!;
		
		for (var i = 1; i < poseOut->Skeleton->Bones.Length; i++) {
			var apply = i == start || HavokPosing.IsBoneDescendantOf(parents, i, start);
			if (!apply) continue;
			
			var relative = HavokPosing.IsBoneDescendantOf(parents, i, end);
			if (!relative) {
				var target = poseOut->ModelPose.Data + i;
				var solved = poseIn->ModelPose[i];
				target->Translation = solved.Translation;
				target->Rotation = solved.Rotation;
				continue;
			}

			// only update child bone transforms (ex fingers, toes) if there's been a change since last frame!
			// only works consistently with end rotation enforced
			// TODO: just fix the local->model math below; this is a bandaid over IK bone drift
			if (
				this.LastPoseInModel != null
				&& this.IkSetup->m_enforceEndRotation
				&& this.LastPoseInModel.Equals(poseInModel)
			) continue;

			var parentId = parents[i];
			var local = HavokPosing.GetLocalTransform(poseIn, i)!;
			var transform = HavokPosing.GetModelTransform(poseOut, parentId)!;
			
			transform.Position += Vector3.Transform(local.Position, transform.Rotation);
			transform.Rotation = Quaternion.Normalize(transform.Rotation * local.Rotation);
			transform.Scale *= local.Scale;
			HavokPosing.SetModelTransform(poseOut, i, transform);
		}
		this.LastPoseInModel = poseInModel;
	}

	private unsafe void ApplyModelPoseDynamic(hkaPose* poseIn, hkaPose* poseOut) {
		var parents = poseOut->Skeleton->ParentIndices;
		var start = this.IkSetup->m_firstJointIdx;
		
		for (var i = 1; i < poseOut->Skeleton->Bones.Length; i++) {
			if (i != start && !HavokPosing.IsBoneDescendantOf(parents, i, start))
				continue;
			*poseOut->AccessBoneModelSpace(i, hkaPose.PropagateOrNot.Propagate) = poseIn->ModelPose[i];
		}
	}
	
	// Disposal
	
	public bool IsDisposed { get; private set; }
	
	public void Dispose() {
		this.LastPoseInModel = null;
		this.IsDisposed = true;
		this.AllocIkSetup.Dispose();
		GC.SuppressFinalize(this);
	}
}
