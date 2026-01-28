using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Utility;

using Ktisis.Common.Utility;
using Ktisis.Data.Files;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Posing.Data;

public class EntityPoseConverter(EntityPose target) {
	public bool IsPoseValid => target.IsValid;
	
	// Save pose file

	public unsafe PoseContainer Save(PoseContainer? filter = null) {
		var bones = new PoseContainer();
		var skeleton = target.GetSkeleton();
		if (skeleton != null)
			bones.Store(skeleton, filter);

		return bones;
	}

	public PoseFile SaveFile() {
		var file = new PoseFile {
			Bones = this.Save()
		};

		// TODO: Weapons

		return file;
	}

	// Apply pose

	public unsafe void Load(
		PoseContainer pose,
		PoseMode mode,
		PoseTransforms transforms
	) {
		var skeleton = target.GetSkeleton();
		if (skeleton == null) return;
		pose.Apply(skeleton, mode, transforms);
	}

	public unsafe void LoadPartial(
		PoseContainer pose,
		int partialIndex,
		PoseTransforms transforms
	) {
		var skeleton = target.GetSkeleton();
		if (skeleton == null) return;
		pose.ApplyToPartial(skeleton, partialIndex, transforms);
	}

	public unsafe void LoadBones(
		PoseContainer pose,
		IEnumerable<PartialBoneInfo> bones,
		PoseTransforms transforms,
		PoseMode modes = PoseMode.All
	) {
		var skeleton = target.GetSkeleton();
		if (skeleton == null) return;
		pose.ApplyToBones(skeleton, bones, transforms, modes);
	}

	public void LoadSelectedBones(
		PoseContainer pose,
		PoseTransforms transforms,
		PoseMode modes,
		bool includeDescendants
	) {
		var selected = this.GetSelectedBones();
		if (includeDescendants)
			selected = this.ExpandToDescendants(selected, modes);
		this.LoadBones(pose, selected, transforms, modes);
	}
	
	public unsafe void LoadReferencePose() {
		var skeleton = target.GetSkeleton();
		if (skeleton == null) return;

		for (var p = 0; p < skeleton->PartialSkeletonCount; p++) {
			var partial = skeleton->PartialSkeletons[p];
			var pose = partial.GetHavokPose(0);
			if (pose == null) continue;

			pose->SetToReferencePose();
			HavokPosing.SyncModelSpace(skeleton, p);
		}
	}
	
	// Filter container

	public unsafe PoseContainer FilterSelectedBones(PoseContainer pose, bool all = true, bool includeDescendants = false, PoseMode modes = PoseMode.All) {
		var result = new PoseContainer();

		var bones = this.GetSelectedBones(all, includeDescendants, modes).ToList();
		if (bones.Count == 0) return result;

		foreach (var bone in bones) {
			if (pose.TryGetValue(bone.Name, out var value))
				result[bone.Name] = value;
		}

		if (bones.All(bone => bone.PartialIndex == 0))
			return result;

		var skeleton = target.GetSkeleton();
		if (skeleton == null || skeleton->PartialSkeletons == null)
			return result;

		for (var p = 1; p < skeleton->PartialSkeletonCount; p++) {
			if(p is (1 or 2) && !modes.HasFlag(PoseMode.Face)) continue;
			if(p is not (1 or 2) && !modes.HasFlag(PoseMode.Body)) continue;

			var partial = skeleton->PartialSkeletons[p];
			var subPose = partial.GetHavokPose(0);
			if (subPose == null || subPose->Skeleton == null) continue;

			var root = subPose->Skeleton->Bones[partial.ConnectedBoneIndex].Name.String;
			if (root.IsNullOrEmpty() || result.ContainsKey(root)) continue;

			if (!pose.TryGetValue(root, out var value)) continue;
			result[root] = value;
		}
		
		return result;
	}

	public PoseContainer FilterExcludeBones(PoseContainer pose, string[] excludes) {
		var result = new PoseContainer();

		var bones = this.GetBones().ToList();
		foreach (var bone in bones) {
			if (!excludes.Contains(bone.Name) && pose.TryGetValue(bone.Name, out var value))
				result[bone.Name] = value;
		}

		return result;
    }

	public IEnumerable<PartialBoneInfo> FilterBonesByModes(IEnumerable<PartialBoneInfo> bones, PoseMode modes) {
		return bones.Where(b => {
			return b.PartialIndex switch {
				1 or 2 => modes.HasFlag(PoseMode.Face),
				_ => modes.HasFlag(PoseMode.Body),
			};
		});
	}
	
	// Pose mapping

	public IEnumerable<PartialBoneInfo> IntersectBonesByName(
		IEnumerable<PartialBoneInfo> second
	) {
		return this.GetBones().IntersectBy(
			second.Select(bone => bone.Name),
			bone => bone.Name
		);
	}
	
	// Iterate bones

	private unsafe IEnumerable<PartialBoneInfo> GetBones() {
		var skeleton = target.GetSkeleton();
		if (skeleton == null || skeleton->PartialSkeletons == null)
			return [];

		var result = new List<PartialBoneInfo>();
		for (var p = 0; p < skeleton->PartialSkeletonCount; p++)
			result.AddRange(this.GetPartialBones(p));
		return result;
	}

	private unsafe IEnumerable<PartialBoneInfo> GetPartialBones(int index) {
		var skeleton = target.GetSkeleton();
		if (skeleton == null || skeleton->PartialSkeletons == null)
			return [];

		var partial = skeleton->PartialSkeletons[index];
		if (partial.HavokPoses.IsEmpty || partial.HavokPoses[0] == 0)
			return [];
		
		return new BoneEnumerator(index, partial).EnumerateBones();
	}
	
	// Iterate selected bones

	public IEnumerable<PartialBoneInfo> GetSelectedBones(bool all = true, bool includeDescendants = false, PoseMode modes = PoseMode.All) {
		var selected = target.Recurse()
			.Prepend(target)
			.Where(entity => entity is SkeletonNode { IsSelected: true })
			.Cast<SkeletonNode>();
		var selectedBones = this.GetBoneSelectionFrom(selected, all).Distinct();
		return includeDescendants ? this.ExpandToDescendants(selectedBones, modes) : selectedBones;
	}

	private IEnumerable<PartialBoneInfo> GetBoneSelectionFrom(IEnumerable<SkeletonNode> nodes, bool all = true) {
		foreach (var node in nodes) {
			switch (node) {
				case BoneNode bone:
					yield return bone.Info;
					continue;
				case SkeletonGroup group:
					foreach (var bone in this.GetBoneSelectionFrom(all ? group.GetAllBones() : group.GetIndividualBones()))
						yield return bone;
					continue;
				default:
					continue;
			}
		}
	}

	public IEnumerable<PartialBoneInfo> ExpandToDescendants(IEnumerable<PartialBoneInfo> bones, PoseMode modes) {
		if(bones == null) return [];

		var result = target.ExpandToDescendants(bones);
		return this.FilterBonesByModes(result, modes);
	}

	public unsafe void FlipPose() {
		var bones = this.GetBones().ToList();
		if(bones.Count == 0) return;

		var skeleton = target.GetSkeleton();

		// Store root for yaw/rot correction after
		var rootBone = bones[0];
		var rootPose = skeleton->PartialSkeletons[rootBone.PartialIndex].GetHavokPose(0);
		var rootInitial = HavokPosing.GetModelTransform(rootPose, rootBone.BoneIndex)!;

		for(var p = 0; p < skeleton->PartialSkeletonCount; p++) {
			if(p is 1 or 2 or 4) continue; // skip face, hair & j_ex bones

			var partial = skeleton->PartialSkeletons[p];
			var pose = partial.GetHavokPose(0);
			if(pose == null || pose->Skeleton == null) continue;

			// Build flipped transforms
			var targets = new Dictionary<string, Quaternion>();
			for(var i = 1; i < pose->Skeleton->Bones.Length; i++) {
				var boneName = pose->Skeleton->Bones[i].Name.String;
				if(boneName.IsNullOrEmpty() || boneName.StartsWith("iv_") || boneName.StartsWith("ya_"))
					continue;
				if(boneName.EndsWith("_l") || boneName.EndsWith("_r")) {
					var oppositeName = boneName.EndsWith("_l") ? boneName[..^1] + "r" : boneName[..^1] + "l";
					if(HavokPosing.TryGetBoneNameIndex(pose, oppositeName) != -1) {
						boneName = oppositeName; // only flip as opposite if it exists
					}
				}

				var transform = HavokPosing.GetModelTransform(pose, i)!;
				targets[boneName] = new Quaternion(-transform.Rotation.X, -transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W);
			}

			// Apply flipped transforms
			for(var i = 1; i < pose->Skeleton->Bones.Length; i++) {
				var boneName = pose->Skeleton->Bones[i].Name.String;
				if(boneName.IsNullOrEmpty() || !targets.TryGetValue(boneName, out var flippedRotation)) continue;

				var initial = HavokPosing.GetModelTransform(pose, i)!;
				var target = new Transform(initial.Position, flippedRotation, initial.Scale);

				HavokPosing.SetModelTransform(pose, i, target);
				HavokPosing.Propagate(skeleton, p, i, target, initial);
			}
		}

		var rootCurrent = HavokPosing.GetModelTransform(rootPose, rootBone.BoneIndex)!;
		var rootTarget = new Transform(rootCurrent.Position, rootCurrent.Rotation, rootCurrent.Scale);

		// Correct root yaw to be relative to foward direction (via difference from initial)
		var initialYaw = HkaEulerAngles.GetYaw(rootInitial.Rotation);
		var currentYaw = HkaEulerAngles.GetYaw(rootCurrent.Rotation);
		var yawDifference = initialYaw - currentYaw;
		var correctionY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yawDifference);
		rootTarget.Rotation = Quaternion.Normalize(correctionY * rootCurrent.Rotation);

		// Correct root flip around Y axis
		var flip180 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
		rootTarget.Rotation = Quaternion.Normalize(flip180 * rootTarget.Rotation);
		
		HavokPosing.SetModelTransform(rootPose, rootBone.BoneIndex, rootTarget);
		HavokPosing.Propagate(skeleton, rootBone.PartialIndex, rootBone.BoneIndex, rootTarget, rootCurrent);
	}
}
