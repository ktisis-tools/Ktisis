using System;
using System.Numerics;

using FFXIVClientStructs.Havok;

using Ktisis.Interop;
using Ktisis.Structs.Havok;

namespace Ktisis.Editor.Posing.Ik;

public class TwoJointsSolver(IkModule module) : IDisposable {
	private readonly Alloc<hkaPose> AllocPose = new(16);
	private readonly Alloc<TwoJointsIkSetup> AllocIkSetup = new(16);

	private unsafe hkaPose* Pose => this.AllocPose.Data;
	
	public unsafe TwoJointsIkSetup* IkSetup => this.AllocIkSetup.Data;
	
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

	public unsafe void SetTargetPosition(Vector3 value) {
		this.IkSetup->m_endTargetMS = new Vector4(value, 0);
	}

	public unsafe void SetTargetRotation(Quaternion value) {
		this.IkSetup->m_endTargetRotationMS = value;
	}
	
	// Pose initialization

	private bool IsInitialized;

	private unsafe void Initialize(hkaPose* pose) {
		if (this.AllocPose.Address == nint.Zero)
			throw new Exception("Allocation for hkaPose failed.");
		
		this.Pose->Skeleton = pose->Skeleton;
		InitArray(&this.Pose->LocalPose);
		InitArray(&this.Pose->ModelPose);
		InitArray(&this.Pose->BoneFlags);
		InitArray(&this.Pose->FloatSlotValues);
		this.Pose->ModelInSync = 0;

		var localSpace = pose->GetSyncedPoseLocalSpace();
		module.InitHkaPose.Invoke(this.Pose, 1, (nint)localSpace, localSpace);
		
		this.IsInitialized = true;
	}

	private unsafe static void InitArray<T>(hkArray<T>* array) where T : unmanaged {
		array->Data = null;
		array->Length = 0;
		*(uint*)(&array->CapacityAndFlags) = 0x80000000;
	}
	
	// Solving

	public unsafe bool Begin(hkaPose* pose, bool frozen = false) {
		if (pose == null || pose->Skeleton == null)
			return false;
		
		if (!this.IsInitialized || this.Pose->Skeleton != pose->Skeleton)
			this.Initialize(pose);
		
		if (!frozen) {
			this.Pose->SetPoseLocalSpace(&pose->LocalPose);
			this.Pose->SyncModelSpace();
		}

		return true;
	}

	public unsafe bool Solve(hkaPose* pose, bool frozen = false) {
		if (frozen) {
			this.Pose->SetToReferencePose();
			this.Pose->SyncModelSpace();
			this.SyncLocal(pose);
		}
		
		byte result = 0;
		module.SolveIk(&result, this.IkSetup, this.Pose);
		
		if (result == 0) return false;
		
		if (frozen)
			this.SyncModel(pose);
		else
			pose->SetPoseModelSpace(this.Pose->AccessSyncedPoseModelSpace());

		return true;
	}

	private unsafe void SyncLocal(hkaPose* pose) {
		var start = this.IkSetup->m_firstJointIdx;
		for (var i = 1; i < this.Pose->Skeleton->Bones.Length; i++) {
			if (i != start && !HavokPosing.IsBoneDescendantOf(pose->Skeleton->ParentIndices, start, i))
				continue;
			*this.Pose->AccessBoneModelSpace(i, hkaPose.PropagateOrNot.Propagate) = pose->ModelPose[i];
		}
	}

	private unsafe void SyncModel(hkaPose* pose) {
		this.Pose->SyncModelSpace();
		
		var parents = pose->Skeleton->ParentIndices;
		hkaSkeletonUtils.transformModelPoseToLocalPose(
			pose->Skeleton->Bones.Length,
			parents.Data,
			pose->ModelPose.Data,
			this.Pose->LocalPose.Data
		);
		
		var start = this.IkSetup->m_firstJointIdx;
		var end = this.IkSetup->m_endBoneIdx;
		
		for (var i = 1; i < pose->Skeleton->Bones.Length; i++) {
			var apply = i == start || HavokPosing.IsBoneDescendantOf(parents, i, start);
			if (!apply) continue;
			
			var relative = HavokPosing.IsBoneDescendantOf(parents, i, end);
			if (!relative) {
				var target = pose->ModelPose.Data + i;
				var solved = this.Pose->ModelPose[i];
				target->Translation = solved.Translation;
				target->Rotation = solved.Rotation;
				continue;
			}
			
			var parentId = parents[i];
			var local = HavokPosing.GetLocalTransform(this.Pose, i)!;
			var transform = HavokPosing.GetModelTransform(pose, parentId)!;
			
			transform.Position += Vector3.Transform(local.Position, transform.Rotation);
			transform.Rotation *= local.Rotation; 
			transform.Scale *= local.Scale;
			HavokPosing.SetModelTransform(pose, i, transform);
		}
	}
	
	// Disposal
	
	public bool IsDisposed { get; private set; }
	
	public void Dispose() {
		this.IsDisposed = true;
		this.AllocPose.Dispose();
		this.AllocIkSetup.Dispose();
		GC.SuppressFinalize(this);
	}
}
