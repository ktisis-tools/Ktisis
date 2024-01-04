namespace Ktisis.Scene.Entities.Skeleton;

public abstract class SkeletonNode : SceneEntity {
	public EntityPose Pose { get; protected init; } = null!;

	protected SkeletonNode(
		ISceneManager scene
	) : base(scene) { }
	
	public int SortPriority { get; set; }

	public void OrderByPriority() {
		this.GetChildren().Sort((_a, _b) => (_a, _b) switch {
			(not SkeletonGroup, SkeletonGroup) => 1,
			(SkeletonGroup, not SkeletonGroup) => -1,
			(SkeletonNode a, SkeletonNode b) => a.SortPriority - b.SortPriority,
			(_, _) => 0
		});
	}
}
