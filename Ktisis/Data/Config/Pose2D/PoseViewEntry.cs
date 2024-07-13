using System.Collections.Generic;

namespace Ktisis.Data.Config.Pose2D;

public record PoseViewEntry {
	public string Name = string.Empty;
	public readonly List<string> Images = new();
	public readonly List<PoseViewBone> Bones = new();
}
