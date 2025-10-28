using System.Collections.Generic;
using System.Linq;

using Dalamud.Utility;

using Ktisis.Data.Files;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Posing.Data;

public class EntityPoseConverter(EntityPose target) {
	public bool IsPoseValid => target.IsValid;
	
	// Save pose file

	public unsafe PoseContainer Save() {
		var bones = new PoseContainer();
		var skeleton = target.GetSkeleton();
		if (skeleton != null)
			bones.Store(skeleton);
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
		PoseTransforms transforms
	) {
		var skeleton = target.GetSkeleton();
		if (skeleton == null) return;
		pose.ApplyToBones(skeleton, bones, transforms);
	}

	public void LoadSelectedBones(
		PoseContainer pose,
		PoseTransforms transforms
	) {
		var selected = this.GetSelectedBones();
		this.LoadBones(pose, selected, transforms);
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

	public unsafe PoseContainer FilterSelectedBones(PoseContainer pose, bool all = true) {
		var result = new PoseContainer();

		var bones = this.GetSelectedBones(all).ToList();
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

	public unsafe PoseContainer FilterExcludeBones(PoseContainer pose, string[] excludes) {
		var result = new PoseContainer();

		var bones = this.GetBones().ToList();
		foreach (var bone in bones) {
			if (!excludes.Contains(bone.Name) && pose.TryGetValue(bone.Name, out var value))
				result[bone.Name] = value;
		}

		return result;
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

		List<PartialBoneInfo> result = [];
		result.AddRange(this.GetPartialBones(0));
		for (var p = 0; p < skeleton->PartialSkeletonCount; p++)
			result.AddRange(this.GetPartialBones(p));
		return result;
	}

	public unsafe IEnumerable<PartialBoneInfo> GetPartialBones(int index) {
		var skeleton = target.GetSkeleton();
		if (skeleton == null || skeleton->PartialSkeletons == null)
			return [];

		var partial = skeleton->PartialSkeletons[index];
		if (partial.HavokPoses.IsEmpty || partial.HavokPoses[0] == 0)
			return [];
		
		return new BoneEnumerator(index, partial).EnumerateBones();
	}
	
	// Iterate selected bones

	public IEnumerable<PartialBoneInfo> GetSelectedBones(bool all = true) {
		var selected = target.Recurse()
			.Prepend(target)
			.Where(entity => entity is SkeletonNode { IsSelected: true })
			.Cast<SkeletonNode>();
		return this.GetBoneSelectionFrom(selected, all).Distinct();
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
}
