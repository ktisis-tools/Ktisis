using System.Collections.Generic;

namespace Ktisis.Data.Config.Props;

public record PropSchema {
	public readonly List<PropEntry> Props = new();
}
