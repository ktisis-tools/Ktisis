using System.Collections.Generic;

using Dalamud.Configuration;

using Ktisis.Data.Config.Bones;
using Ktisis.Data.Config.Display;

namespace Ktisis.Data.Config;

public class ConfigFile : IPluginConfiguration {
	// Version

	public const int CurrentVersion = 4;

	public int Version { get; set; } = CurrentVersion;

	// Item display

	public Categories Categories = new() {
		Default = new BoneCategory("Other")
	};

	public Dictionary<ItemType, ItemDisplay> Display = ItemDisplay.GetDefaults();
}
