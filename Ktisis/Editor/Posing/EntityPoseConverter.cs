using System.Collections.Generic;
using System.Linq;

using Ktisis.Data.Files;
using Ktisis.Editor.Posing.Partials;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Posing;

public class EntityPoseConverter(EntityPose target) {
	// Save pose file

	public unsafe PoseFile Save() {
		var file = new PoseFile();

		var skeleton = target.GetSkeleton();
		if (skeleton != null) {
			file.Bones = new PoseContainer();
			file.Bones.Store(skeleton);
		}
		
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
	
	// Iterate bones

	public IEnumerable<PartialBoneInfo> GetBones()
		=> this.GetBones(target.Children);

	private IEnumerable<PartialBoneInfo> GetBones(IEnumerable<SceneEntity> entities) {
		foreach (var entity in entities) {
			switch (entity) {
				case BoneNode bone:
					yield return bone.Info;
					continue;
				case BoneNodeGroup group:
					foreach (var bone in this.GetBones(group.Children))
						yield return bone;
					continue;
				default:
					continue;
			}
		}
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
