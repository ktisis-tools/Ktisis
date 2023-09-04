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
	
	public Bone? GetBoneByLowestIndex() {
		var lowP = 0;
		var lowIx = 0;
		Bone? result = null;
		foreach (var item in RecurseChildren()) {
			var bone = item switch {
				ArmatureGroup group => group.GetBoneByLowestIndex(),
				Bone _bone => _bone,
				_ => null
			};

			if (bone is null) continue;

			var boneIx = bone.Data.BoneIndex;
			var partIx = bone.Data.PartialIndex;
			var set = result is null || partIx < lowP || boneIx < lowIx;
			if (!set) continue;
            
			lowP = partIx;
			lowIx = boneIx;
			result = bone;
		}
		
		return result;
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