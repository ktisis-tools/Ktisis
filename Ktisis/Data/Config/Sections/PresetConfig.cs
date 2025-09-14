using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Ktisis.Data.Config.Sections;

public class PresetConfig {
	internal delegate void PresetRemoved(string presetName);
	internal static PresetRemoved? PresetRemovedEvent;

	public SortedDictionary<string, ImmutableHashSet<string>> Presets = new ();
	public List<string> DefaultPresets = new List<string>();

	public bool PresetIsDefault(string name) {
		return this.DefaultPresets.FirstOrDefault(x => x == name) != null;
	}
}
