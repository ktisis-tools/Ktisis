using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using FFXIVClientStructs.Havok;

using Ktisis.Data.Config.Bones;
using Ktisis.Scene.Decor;

namespace Ktisis.Editor.Posing.Ik;

public interface IIkController {
	public void Setup(ISkeleton skeleton);

	public bool TrySetupGroup(string name, TwoJointsGroupParams param, out TwoJointsGroup? group);

	public void Solve(bool frozen = false);
	
	public void Destroy();
}

public class IkController : IIkController {
	private readonly IkModule _module;
	private readonly TwoJointsSolver _twoJoints;

	private ISkeleton? Skeleton;

	public IkController(
		IkModule module
	) {
		this._module = module;
		this._twoJoints = new TwoJointsSolver(module);
	}

	public void Setup(ISkeleton skeleton) {
		this.Skeleton = skeleton;
		this._twoJoints.Setup();
	}
	
	// Groups
	
	private readonly Dictionary<string, TwoJointsGroup> Groups = new();

	public unsafe bool TrySetupGroup(string name, TwoJointsGroupParams param, out TwoJointsGroup? group) {
		group = null;
		
		Ktisis.Log.Verbose($"Setting up IK group: {name}");

		if (this.Skeleton == null) return false;

		var skeleton = this.Skeleton.GetSkeleton();
		if (skeleton == null) return false;
		
		var partial = skeleton->PartialSkeletons[0];
		if (partial.SkeletonResourceHandle == null || partial.HavokPoses == null)
			return false;
		
		var pose = partial.GetHavokPose(0);
		if (pose == null || pose->Skeleton == null)
			return false;

		if (!this.Groups.TryGetValue(name, out group)) {
			group = new TwoJointsGroup {
				HingeAxis = param.Type == TwoJointsType.Leg ? -Vector3.UnitZ : Vector3.UnitZ
			};
		}

		var first = TryResolveBone(pose, param.FirstBone);
		var second = TryResolveBone(pose, param.SecondBone);
		var last = TryResolveBone(pose, param.EndBone);
		if (first == -1 || second == -1 || last == -1) return false;

		group.FirstBoneIndex = first;
		group.FirstTwistIndex = TryResolveBone(pose, param.FirstTwist);
		group.SecondBoneIndex = second;
		group.SecondTwistIndex = TryResolveBone(pose, param.SecondTwist);
		group.EndBoneIndex = last;
		
		Ktisis.Log.Verbose($"Resolved bones: {first} {second} {last} ({group.FirstTwistIndex}, {group.SecondTwistIndex})");
		
		group.SkeletonId = partial.SkeletonResourceHandle->ResourceHandle.Id;

		this.Groups[name] = group;
		return true;
	}

	private unsafe static short TryResolveBone(hkaPose* pose, IEnumerable<string> names) => names
		.Select(name => HavokPosing.TryGetBoneNameIndex(pose, name))
		.FirstOrDefault(index => index != -1, (short)-1);
	
	// Solvers

	public unsafe void Solve(bool frozen = false) {
		if (this.Skeleton == null) return;

		var skeleton = this.Skeleton.GetSkeleton();
		if (skeleton == null || skeleton->PartialSkeletons == null)
			return;

		var partial = skeleton->PartialSkeletons[0];
		if (partial.HavokPoses == null || partial.SkeletonResourceHandle == null)
			return;
		
		var pose = partial.GetHavokPose(0);
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
