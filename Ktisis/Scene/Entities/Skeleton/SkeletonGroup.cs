using System.Collections.Generic;
using System.Linq;

using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton;

public abstract class SkeletonGroup(ISceneManager scene) : SkeletonNode(scene), IVisibility {
	public bool Visible {
		get => this.RecurseVisible().All(vis => vis.Visible);
		set {
			foreach (var child in this.RecurseVisible())
				child.Visible = value;
		}
	}
	
	protected IEnumerable<IVisibility> RecurseVisible()
		=> this.Children.Where(child => child is IVisibility).Cast<IVisibility>();

	protected void Clean(int pIndex, uint pId) {
		var staleItems = this.GetChildren().Where(item => {
			switch (item) {
				case BoneNodeGroup group:
					group.Clean(pIndex, pId);
					return group.IsStale();
				case BoneNode bone:
					return bone.Info.PartialIndex == pIndex && bone.PartialId != pId;
				default:
					return false;
			}
		}).ToList();

		foreach (var staleItem in staleItems)
			staleItem.Remove();
	}

	public IEnumerable<BoneNode> GetAllBones() {
		var unique = new HashSet<BoneNode>();
		foreach (var node in this.Children) {
			switch (node) {
				case BoneNode bone:
					if (unique.Add(bone))
						yield return bone;
					break;
				case SkeletonGroup group:
					foreach (var bone in group.GetAllBones())
						yield return bone;
					break;
				default:
					continue;
			}
		}
	}
	
	public IEnumerable<BoneNode> GetIndividualBones() {
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
			var partial = pose.GetPartialInfo(partialIx);
			if (partial == null) return false;

			var parent = partial
				.GetParentsOf(boneIx)
				.Any(parentId => results.Any(x => x.MatchesId(partialIx, parentId)));

			if (parent) return true;
			if (partialIx == 0) return false;

			var rootPartial = pose.GetPartialInfo(0);
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
