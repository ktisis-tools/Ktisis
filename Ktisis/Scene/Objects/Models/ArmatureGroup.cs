namespace Ktisis.Scene.Objects.Models;

public abstract class ArmatureGroup : ArmatureNode {
	protected void Clean(int pIndex, uint pId) => this.Children.RemoveAll(item => {
		switch (item) {
			case BoneGroup group:
				group.Clean(pIndex, pId);
				return group.IsStale();
			case Bone bone:
				return bone.PartialIndex == pIndex && bone.PartialId != pId;
			default:
				return false;
		}
	});
}