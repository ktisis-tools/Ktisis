using System.Collections.Generic;
using System.Collections.Immutable;

namespace Ktisis.Data.Config.Sections;

public class PresetConfig {
	internal delegate void PresetRemoved(string presetName);
	internal static PresetRemoved? PresetRemovedEvent;

	public SortedDictionary<string, ImmutableHashSet<string>> Presets = new ();
}
