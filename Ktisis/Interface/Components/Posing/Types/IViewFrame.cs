using Ktisis.Data.Config.Pose2D;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Interface.Components.Posing.Types;

public interface IViewFrame {
	public void DrawView(PoseViewEntry entry, float width, float height);
	public void DrawBones(EntityPose pose);
}
