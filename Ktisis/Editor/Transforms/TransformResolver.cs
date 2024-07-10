using System.Collections.Generic;
using System.Linq;

using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Editor.Transforms;

public static class TransformResolver {
	public static SceneEntity? GetPoseTarget(IEnumerable<SceneEntity> entities) {
		BoneNode? target = null;

		var bones = entities
			.Where(item => item is BoneNode)
			.Cast<BoneNode>();

		foreach (var bone in bones) {
			if (target == null) {
				target = bone;
				continue;
			}

			var pose = bone.Pose;
			if (pose != target.Pose)
				continue;

			var partialIx = bone.Info.PartialIndex;
			var partial = pose.GetPartialInfo(partialIx);
			if (partial == null) continue;

			int? potentialParent = (partialIx, target.Info.PartialIndex) switch {
				var (p, t) when p == t => target.Info.BoneIndex,
				var (p, t) when p < t => pose.GetPartialInfo(t)?.ConnectedParentBoneIndex,
				_ => null
			};
			
			if (
				potentialParent != null
				&& (partial.IsBoneDescendantOf(potentialParent.Value, bone.Info.BoneIndex)
				|| (target.Info.ParentIndex == bone.Info.ParentIndex && bone.Info.BoneIndex < target.Info.BoneIndex))
			) {
				target = bone;
			}
		}

		return target;
	}
	
	public static IEnumerable<SceneEntity> GetCorrelatingBones(IEnumerable<SceneEntity> entities, bool yieldDefault = false) {
		var unique = new HashSet<BoneNode>();
		foreach (var node in entities) {
			switch (node) {
				case BoneNode bone:
					if (unique.Add(bone))
						yield return bone;
					break;
				case SkeletonGroup group:
					foreach (var bone in group.GetIndividualBones().Where(bone => unique.Add(bone)))
						yield return bone;
					break;
				default:
					if (yieldDefault)
						yield return node;
					continue;
			}
		}
	}

	public static Dictionary<EntityPose, Dictionary<int, List<BoneNode>>> BuildPoseMap(
		SceneEntity? target,
		IEnumerable<SceneEntity> entities
	) {
		var map = new Dictionary<EntityPose, Dictionary<int, List<BoneNode>>>();

		var bones = GetCorrelatingBones(entities).Cast<BoneNode>();
		foreach (var bone in bones) {
			var pose = bone.Pose;
			if (pose == target) continue;

			var dictExists = map.TryGetValue(pose, out var dict);
			dict ??= new Dictionary<int, List<BoneNode>>();

			var partialIx = bone.Info.PartialIndex;
			var listExists = dict.TryGetValue(partialIx, out var list);
			list ??= new List<BoneNode>();
			list.Add(bone);
			
			if (!listExists) dict.Add(partialIx, list);
			if (!dictExists) map.Add(pose, dict);
		}
		
		return map;
	}
}
