using System;
using System.Numerics;

using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Container.Array;

using Ktisis.Common.Extensions;
using Ktisis.Interop;
using Ktisis.Structs.Havok;

namespace Ktisis.Editor.Posing.Ik.Ccd;

public class CcdSolver : IDisposable {
	private readonly IkModule _module;
	
	private readonly Alloc<CcdIkSolver> AllocSolver;
	
	private readonly Alloc<CcdIkConstraint> AllocIkConstraint = new(16);
	private readonly Alloc<hkArray<CcdIkConstraint>> AllocHkArray = new(16);
	
	private unsafe CcdIkSolver* IkSolver => this.AllocSolver.Data;
	
	public unsafe CcdIkConstraint* IkConstraint => this.AllocIkConstraint.Data;

	public CcdSolver(
		IkModule module,
		Alloc<CcdIkSolver> solver
	) {
		this._module = module;
		this.AllocSolver = solver;
	}
	
	// Setup

	public unsafe void Setup() {
		if (this.AllocIkConstraint.Address == nint.Zero)
			throw new Exception("Allocation for IkConstraint failed.");

		this.IkConstraint->m_startBone = -1;
		this.IkConstraint->m_endBone = -1;
		this.IkConstraint->m_targetMS = Vector4.Zero;
		HavokEx.Initialize(this.AllocHkArray.Data, this.IkConstraint, 1);
	}
	
	// Solving

	public unsafe void Solve(hkaPose* poseIn, hkaPose* poseOut, bool frozen = false) {
		if (poseOut == null || poseOut->Skeleton == null)
			return;
		
		if (frozen) {
			poseIn->SetToReferencePose();
			poseIn->SyncModelSpace();
			this.UpdateModelPose(poseIn, poseOut);
		}

		byte result = 0;
		this._module.SolveCcd(this.IkSolver, &result, this.AllocHkArray.Data, poseIn);

		poseIn->SyncModelSpace();
		if (frozen)
			this.ApplyModelPoseStatic(poseIn, poseOut);
		else
			this.ApplyModelPoseDynamic(poseIn, poseOut);
	}
	
	public unsafe void SolveGroup(hkaPose* poseIn, hkaPose* poseOut, CcdGroup group, bool frozen = false) {
		if (!group.IsEnabled) return;
		
		var ik = this.IkSolver;
		var param = this.IkConstraint;

		param->m_startBone = group.StartBoneIndex;
		param->m_endBone = group.EndBoneIndex;
		param->m_targetMS = new Vector4(group.TargetPosition, 0.0f);
		
		ik->m_iterations = group.Iterations;
		ik->m_gain = group.Gain;
		
		this.Solve(poseIn, poseOut, frozen);
	}
	
	private unsafe void UpdateModelPose(hkaPose* poseIn, hkaPose* poseOut) {
		var start = this.IkConstraint->m_startBone;
		for (var i = 1; i < poseIn->Skeleton->Bones.Length; i++) {
			if (!HavokPosing.IsBoneDescendantOf(poseOut->Skeleton->ParentIndices, start, i))
				continue;
			*poseIn->AccessBoneModelSpace(i, hkaPose.PropagateOrNot.Propagate) = poseOut->ModelPose[i];
		}
	}

	private unsafe void ApplyModelPoseStatic(hkaPose* poseIn, hkaPose* poseOut) {
		var parents = poseOut->Skeleton->ParentIndices;
		var start = this.IkConstraint->m_startBone;
		
		for (var i = 1; i < poseOut->Skeleton->Bones.Length; i++) {
			var apply = i == start || HavokPosing.IsBoneDescendantOf(parents, i, start);
			if (!apply) continue;

			var transform = HavokPosing.GetModelTransform(poseIn, i)!;
			HavokPosing.SetModelTransform(poseOut, i, transform);
		}
	}

	private unsafe void ApplyModelPoseDynamic(hkaPose* poseIn, hkaPose* poseOut) {
		var parents = poseOut->Skeleton->ParentIndices;
		var start = this.IkConstraint->m_startBone;
		
		for (var i = 1; i < poseOut->Skeleton->Bones.Length; i++) {
			if (i != start && !HavokPosing.IsBoneDescendantOf(parents, i, start))
				continue;
			*poseOut->AccessBoneModelSpace(i, hkaPose.PropagateOrNot.Propagate) = poseIn->ModelPose[i];
		}
	}
	
	// Disposal

	public void Dispose() {
		this.AllocSolver.Dispose();
		this.AllocIkConstraint.Dispose();
		this.AllocHkArray.Dispose();
		GC.SuppressFinalize(this);
	}
}
