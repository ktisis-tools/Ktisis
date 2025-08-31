using System.Collections.Generic;
using System.Collections.Immutable;

namespace Ktisis.Data.Config.Sections;

public class PresetConfig {
	public SortedDictionary<string, ImmutableHashSet<string>> Presets = new ();
}
