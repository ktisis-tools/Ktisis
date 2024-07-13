using System.Collections.Generic;

namespace Ktisis.Data.Config.Pose2D;

public record PoseViewSchema {
	public readonly Dictionary<string, PoseViewEntry> Views = new();
}
