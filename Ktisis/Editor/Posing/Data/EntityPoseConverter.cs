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
		PoseTransforms transforms
	) {
		var skeleton = target.GetSkeleton();
		if (skeleton == null) return;
		pose.Apply(skeleton, transforms);
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
	
	// Filter container

	public unsafe PoseContainer FilterSelectedBones(PoseContainer pose) {
		var result = new PoseContainer();

		var bones = this.GetSelectedBones().ToList();
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

	private unsafe IEnumerable<PartialBoneInfo> GetPartialBones(int index) {
		var skeleton = target.GetSkeleton();
		if (skeleton == null || skeleton->PartialSkeletons == null)
			return [];

		var partial = skeleton->PartialSkeletons[index];
		if (partial.HavokPoses == null || partial.HavokPoses[0] == 0)
			return [];
		
		return new BoneEnumerator(index, partial).EnumerateBones();
	}
	
	// Iterate selected bones

	public IEnumerable<PartialBoneInfo> GetSelectedBones() {
		var selected = target.Recurse()
			.Prepend(target)
			.Where(entity => entity is SkeletonNode { IsSelected: true })
			.Cast<SkeletonNode>();
		return this.GetBoneSelectionFrom(selected).Distinct();
	}

	private IEnumerable<PartialBoneInfo> GetBoneSelectionFrom(IEnumerable<SkeletonNode> nodes) {
		foreach (var node in nodes) {
			switch (node) {
				case BoneNode bone:
					yield return bone.Info;
					continue;
				case SkeletonGroup group:
					foreach (var bone in this.GetBoneSelectionFrom(group.GetAllBones()))
						yield return bone;
					continue;
				default:
					continue;
			}
		}
	}
}
