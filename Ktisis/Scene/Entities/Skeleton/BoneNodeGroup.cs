using Ktisis.Data.Config.Bones;
using Ktisis.Scene.Types;

namespace Ktisis.Scene.Entities.Skeleton;

public class BoneNodeGroup : SkeletonGroup {
	public BoneCategory? Category { get; set; }

	public BoneNodeGroup(
		ISceneManager scene,
		EntityPose pose
	) : base(scene) {
		this.Type = EntityType.BoneGroup;
		this.Pose = pose;
	}

	public bool IsStale() => !this.IsValid || this.GetChildren().Count == 0;
}
