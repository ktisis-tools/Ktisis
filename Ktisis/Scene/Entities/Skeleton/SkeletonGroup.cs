using System.Collections.Generic;
using System.Linq;

using Ktisis.Editor.Strategy;
using Ktisis.Editor.Strategy.Bones;

namespace Ktisis.Scene.Entities.Skeleton;

public abstract class SkeletonGroup : SkeletonNode {
	protected SkeletonGroup(
		ISceneManager scene
	) : base(scene) {
		this.Strategy = new GroupEditor(this);
	}

	protected void Clean(int pIndex, uint pId) => this.GetChildren().RemoveAll(item => {
		switch (item) {
			case BoneNodeGroup group:
				group.Clean(pIndex, pId);
				return group.IsStale();
			case BoneNode bone:
				return bone.Info.PartialIndex == pIndex && bone.PartialId != pId;
			default:
				return false;
		}
	});
	
	public List<BoneNode> GetIndividualBones() {
		var results = new List<BoneNode>();
		foreach (var item in this.Recurse()) {
			switch (item) {
				case BoneNodeGroup group:
					results.AddRange(group.GetIndividualBones());
					break;
				case BoneNode bone:
					results.Add(bone);
					break;
				default:
					continue;
			}
		}

		var pose = this.Pose;
		results = results.Distinct().ToList();
		results.RemoveAll(bone => {
			var boneIx = bone.Info.BoneIndex;

			var partialIx = bone.Info.PartialIndex;
			var partial = pose.GetPartial(partialIx);
			if (partial == null) return false;

			var parent = partial
				.GetParentsOf(boneIx)
				.Any(parentId => results.Any(x => x.MatchesId(partialIx, parentId)));

			if (parent) return true;
			if (partialIx == 0) return false;

			var rootPartial = pose.GetPartial(0);
			if (rootPartial == null) return false;

			var connIx = partial.ConnectedParentBoneIndex;
			return rootPartial
				.GetParentsOf(connIx)
				.Prepend(connIx)
				.Any(id => results.Any(x => x.MatchesId(0, id)));
		});

		return results;
	}
}
