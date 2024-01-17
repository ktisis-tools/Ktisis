using System.Linq;

using Ktisis.Data.Config.Bones;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton;

public class BoneNodeGroup : SkeletonGroup, IAttachTarget {
	public BoneCategory? Category { get; set; }

	public BoneNodeGroup(
		ISceneManager scene,
		EntityPose pose
	) : base(scene) {
		this.Type = EntityType.BoneGroup;
		this.Pose = pose;
	}

	public bool IsStale() => !this.IsValid || this.GetChildren().Count == 0;
	
	// Attach
	
	public void AcceptAttach(IAttachable child) {
		var target = this.GetIndividualBones()
			.Where(bone => bone.Info.PartialIndex == 0)
			.MinBy(bone => bone.Info.BoneIndex);
		target?.AcceptAttach(child);
	}
}
