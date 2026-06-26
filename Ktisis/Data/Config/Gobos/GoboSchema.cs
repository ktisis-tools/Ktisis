using System.Collections.Generic;

namespace Ktisis.Data.Config.Gobos;

public record GoboSchema {
	public readonly List<GoboEntry> Gobos = new();
}
