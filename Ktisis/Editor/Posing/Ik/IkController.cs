using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using FFXIVClientStructs.Havok.Animation.Rig;

using Ktisis.Common.Extensions;
using Ktisis.Data.Config.Bones;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Ik.Ccd;
using Ktisis.Editor.Posing.Ik.TwoJoints;
using Ktisis.Editor.Posing.Ik.Types;
using Ktisis.Interop;
using Ktisis.Scene.Decor;

namespace Ktisis.Editor.Posing.Ik;

public interface IIkController {
	public void Setup(ISkeleton skeleton);

	public int GroupCount { get; }
	public IEnumerable<(string name, IIkGroup group)> GetGroups();
	public bool TrySetupGroup(string name, CcdGroupParams param, out CcdGroup? group);
	public bool TrySetupGroup(string name, TwoJointsGroupParams param, out TwoJointsGroup? group);

	public void Solve(bool frozen = false);
	
	public void Destroy();
}

public class IkController : IIkController {
	private readonly IkModule _module;

	private ISkeleton? Skeleton;

	public IkController(
		IkModule module,
		CcdSolver ccd,
		TwoJointsSolver twoJoints
	) {
		this._module = module;
		this._ccd = ccd;
		this._twoJoints = twoJoints;
	}

	public void Setup(ISkeleton skeleton) {
		this.Skeleton = skeleton;
	}
	
	// Pose
	
	private readonly Alloc<hkaPose> _allocPose = new(16);
	private unsafe hkaPose* Pose => this._allocPose.Data;

	private bool IsInitialized;

	private unsafe void Initialize(hkaPose* pose) {
		if (this._allocPose.Address == nint.Zero)
			throw new Exception("Allocation for hkaPose failed.");
		
		this.Pose->Skeleton = pose->Skeleton;
		HavokEx.Initialize(&this.Pose->LocalPose);
		HavokEx.Initialize(&this.Pose->ModelPose);
		HavokEx.Initialize(&this.Pose->BoneFlags);
		HavokEx.Initialize(&this.Pose->FloatSlotValues);
		this.Pose->ModelInSync = 0;
		
		var localSpace = pose->GetSyncedPoseLocalSpace();
		this._module.InitHkaPose.Invoke(this.Pose, 1, (nint)localSpace, localSpace);
		
		this.IsInitialized = true;
	}
	
	// Solvers
	
	private readonly CcdSolver _ccd;
	private readonly TwoJointsSolver _twoJoints;

	public unsafe void Solve(bool frozen = false) {
		if (this.Skeleton == null) return;

		var skeleton = this.Skeleton.GetSkeleton();
		if (skeleton == null || skeleton->PartialSkeletons == null)
			return;

		var partial = skeleton->PartialSkeletons[0];
		if (partial.HavokPoses.IsEmpty || partial.SkeletonResourceHandle == null)
			return;
		
		var pose = partial.GetHavokPose(0);
		if (pose == null || pose->Skeleton == null) return;
		
		var id = partial.SkeletonResourceHandle->Id;
		
		var groups = this.Groups.Values
			.Where(group => group.IsEnabled && group.SkeletonId == id)
			.ToList();
		
		if (groups.Count == 0)
			return;

		this.Solve(pose, groups, frozen);
	}

	private unsafe void Solve(hkaPose* pose, IEnumerable<IIkGroup> groups, bool frozen) {
		if (!this.IsInitialized || pose->Skeleton != this.Pose->Skeleton)
			this.Initialize(pose);

		if (!frozen) {
			this.Pose->SetPoseLocalSpace(&pose->LocalPose);
			this.Pose->SyncModelSpace();
		}
		
		foreach (var group in groups) {
			switch (group) {
				case TwoJointsGroup tj:
					this._twoJoints.SolveGroup(this.Pose, pose, tj, frozen);
					break;
				case CcdGroup ccd:
					this._ccd.SolveGroup(this.Pose, pose, ccd, frozen);
					break;
			}
		}
	}
	
	// Groups
	
	private readonly Dictionary<string, IIkGroup> Groups = new();

	public int GroupCount => this.Groups.Count;

	public IEnumerable<(string name, IIkGroup group)> GetGroups()
		=> this.Groups.Select(pair => (pair.Key, pair.Value));

	public unsafe bool TrySetupGroup(string name, CcdGroupParams param, out CcdGroup? group) {
		group = null;
		
		Ktisis.Log.Verbose($"Setting up group for CCD IK: {name}");

		if (this.Skeleton == null || SkeletonPoseData.TryGet(this.Skeleton, 0, 0) is not { } data)
			return false;
		
		if (this.Groups.TryGetValue(name, out var value))
			group = value as CcdGroup;
		group ??= new CcdGroup();

		var start = data.TryResolveBone(param.StartBone);
		var end = data.TryResolveBone(param.EndBone);
		if (start == -1 || end == -1) {
			Ktisis.Log.Warning($"Resolve failed: {start} {end}");
			return false;
		}

		group.StartBoneIndex = start;
		group.EndBoneIndex = end;
		
		Ktisis.Log.Verbose($"Resolved bones: {start} {end}");

		group.SkeletonId = data.Partial.SkeletonResourceHandle->Id;

		this.Groups[name] = group;
		return true;
	}

	public unsafe bool TrySetupGroup(string name, TwoJointsGroupParams param, out TwoJointsGroup? group) {
		group = null;
		
		Ktisis.Log.Verbose($"Setting up group for TwoJoints IK: {name}");

		if (this.Skeleton == null || SkeletonPoseData.TryGet(this.Skeleton, 0, 0) is not {} data)
			return false;
		
		if (this.Groups.TryGetValue(name, out var value))
			group = value as TwoJointsGroup;
		group ??= new TwoJointsGroup {
			HingeAxis = param.Type == TwoJointsType.Leg ? -Vector3.UnitZ : Vector3.UnitZ
		};

		var first = data.TryResolveBone(param.FirstBone);
		var second = data.TryResolveBone(param.SecondBone);
		var last = data.TryResolveBone(param.EndBone);
		if (first == -1 || second == -1 || last == -1) return false;

		group.FirstBoneIndex = first;
		group.FirstTwistIndex = data.TryResolveBone(param.FirstTwist);
		group.SecondBoneIndex = second;
		group.SecondTwistIndex = data.TryResolveBone(param.SecondTwist);
		group.EndBoneIndex = last;
		
		Ktisis.Log.Verbose($"Resolved bones: {first} {second} {last} ({group.FirstTwistIndex}, {group.SecondTwistIndex})");
		
		group.SkeletonId = data.Partial.SkeletonResourceHandle->Id;

		this.Groups[name] = group;
		return true;
	}

	// Disposal
	
	private bool _isDestroyed;

	public void Destroy() {
		if (this._isDestroyed)
			throw new Exception("IK controller is already disposed.");
		this._ccd.Dispose();
		this._twoJoints.Dispose();
		this._isDestroyed = this._module.RemoveController(this);
	}
}
