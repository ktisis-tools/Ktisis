using System.Linq;
using System.Collections.Generic;

using Ktisis.Scene.Impl;

namespace Ktisis.Scene.Objects.Models;

public abstract class ArmatureGroup : ArmatureNode, IVisibility {
	// Armature
	
	protected void Clean(int pIndex, uint pId) => this.Children.RemoveAll(item => {
		switch (item) {
			case BoneGroup group:
				group.Clean(pIndex, pId);
				return group.IsStale();
			case Bone bone:
				return bone.Data.PartialIndex == pIndex && bone.PartialId != pId;
			default:
				return false;
		}
	});

	public List<Bone> GetIndividualBones() {
		var results = new List<Bone>();
		foreach (var item in RecurseChildren()) {
			switch (item) {
				case ArmatureGroup group:
					results.AddRange(group.GetIndividualBones());
					break;
				case Bone bone:
					results.Add(bone);
					break;
				default:
					continue;
			}
		}

        var armature = GetArmature();
		results = results.Distinct().ToList();
		results.RemoveAll(bone => {
			var boneIx = bone.Data.BoneIndex;
			
			var partialIx = bone.Data.PartialIndex;
			var partial = armature.GetPartialCache(partialIx);
			if (partial is null) return false;

			var parent = partial.GetParentsOf(boneIx)
				.Any(parentId => results.Any(x => x.MatchesId(partialIx, parentId)));

			if (parent)
				return true;

			if (partialIx == 0)
				return false;
			
            var rootPartial = armature.GetPartialCache(0);
            if (rootPartial is null)
                return false;

            var connIx = partial.ConnectedParentBoneIndex;
            return rootPartial.GetParentsOf(connIx)
	            .Prepend(connIx)
	            .Any(id => results.Any(x => x.MatchesId(0, id)));
		});

		return results;
	}

	// IPoseObject

	public new bool Visible {
		get {
			var visible = this.Count > 0;
			foreach (var child in GetChildren()) {
				if (child is not IVisibility iVis) continue;
				visible &= iVis.Visible;
				if (!visible) break;
			}
			return visible;
		}
		set {
			foreach (var child in GetChildren()) {
				if (child is not IVisibility iVis) continue;
				iVis.SetVisible(value);
			}
		}
	}

	public bool SetVisible(bool visible) => this.Visible = visible;
}
