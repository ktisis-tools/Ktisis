using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

using Ktisis.Data.Config.Bones;

namespace Ktisis.Editor.Posing.Ik;

public interface IIkController {
	public unsafe void Update(Skeleton* skeleton);

	public bool TrySetupGroup(string name, TwoJointsGroupParams param, out TwoJointsGroup? group);

	public void Solve(bool frozen = false);
	
	public void Destroy();
}

public class IkController : IIkController {
	private readonly IkModule _module;
	private readonly TwoJointsSolver _twoJoints;
	
	private unsafe Skeleton* Skeleton;

	public IkController(IkModule module) {
		this._module = module;
		this._twoJoints = new TwoJointsSolver(module);
	}

	public void Setup() {
		this._twoJoints.Setup();
	}

	public unsafe void Update(Skeleton* skeleton) {
		this.Skeleton = skeleton;
	}
	
	// Groups
	
	private readonly Dictionary<string, TwoJointsGroup> Groups = new();

	public unsafe bool TrySetupGroup(string name, TwoJointsGroupParams param, out TwoJointsGroup? group) {
		group = null;
		
		Ktisis.Log.Verbose($"Setting up IK group: {name}");
		
		var partial = this.Skeleton->PartialSkeletons[0];
		if (partial.SkeletonResourceHandle == null) return false;
		
		var pose = partial.HavokPoses != null ? partial.GetHavokPose(0) : null;
		if (pose == null || pose->Skeleton == null) return false;

		if (!this.Groups.TryGetValue(name, out group))
			group = new TwoJointsGroup();

		var first = TryResolveBone(pose, param.FirstBone);
		var second = TryResolveBone(pose, param.SecondBone);
		var last = TryResolveBone(pose, param.EndBone);
		if (first == -1 || second == -1 || last == -1) return false;

		group.FirstBoneIndex = first;
		group.FirstTwistIndex = TryResolveBone(pose, param.FirstTwist);
		group.SecondBoneIndex = second;
		group.SecondTwistIndex = TryResolveBone(pose, param.SecondTwist);
		group.EndBoneIndex = last;
		
		group.SkeletonId = partial.SkeletonResourceHandle->ResourceHandle.Id;

		this.Groups[name] = group;
		return true;
	}

	private unsafe static short TryResolveBone(hkaPose* pose, IEnumerable<string> names) => names
		.Select(name => HavokPosing.TryGetBoneNameIndex(pose, name))
		.FirstOrDefault(index => index != -1, (short)-1);
	
	// Solvers

	public unsafe void Solve(bool frozen = false) {
		if (this.Skeleton == null || this.Skeleton->PartialSkeletons == null)
			return;

		var partial = this.Skeleton->PartialSkeletons[0];
		if (partial.SkeletonResourceHandle == null) return;
		
		var pose = partial.HavokPoses != null ? partial.GetHavokPose(0) : null;
		if (pose == null || pose->Skeleton == null) return;
		
		var id = partial.SkeletonResourceHandle->ResourceHandle.Id;
		
		var groups = this.Groups.Values
			.Where(group => group.IsEnabled && group.SkeletonId == id)
			.ToList();
		
		if (groups.Count == 0 || !this._twoJoints.Begin(pose, frozen))
			return;
		
		foreach (var group in groups)
			this.SolveGroup(pose, group, frozen);
	}

	private unsafe void SolveGroup(hkaPose* pose, TwoJointsGroup group, bool frozen = false) {
		var ik = this._twoJoints.IkSetup;
		
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
		
		var target = HavokPosing.GetModelTransform(pose, group.EndBoneIndex);
		if (target == null) return;

		var isRelative = group.Mode == TwoJointsMode.Relative;
		if (isRelative || !group.EnforcePosition)
			group.TargetPosition = target.Position;
		if (isRelative || !group.EnforceRotation)
			group.TargetRotation = target.Rotation;
		
		ik->m_endTargetMS = new Vector4(group.TargetPosition, 0.0f);
		ik->m_endTargetRotationMS = group.TargetRotation;

		this._twoJoints.Solve(pose, frozen);
	}

	// Disposal
	
	private bool _isDestroyed;

	public void Destroy() {
		if (this._isDestroyed)
			throw new Exception("IK controller is already disposed.");
		this._isDestroyed = this._module.RemoveController(this);
	}
}
