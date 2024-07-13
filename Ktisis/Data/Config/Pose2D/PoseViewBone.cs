using System.Numerics;

namespace Ktisis.Data.Config.Pose2D;

public record PoseViewBone {
	public string Label = string.Empty;
	public string Name = string.Empty;
	public Vector2 Position = Vector2.Zero;
}
