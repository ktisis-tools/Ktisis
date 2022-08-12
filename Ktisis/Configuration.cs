using System;

using Dalamud.Plugin;
using Dalamud.Configuration;

namespace Ktisis {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public int Version { get; set; } = 0;

		// Plugin settings & preferences

		public bool ShowOnEnterGpose { get; set; } = true;

		public bool AllowAxisFlip { get; set; } = true;

		// UI memory

		public bool ShowSkeleton { get; set; } = false;

		// save

		public void Save(Ktisis plugin) {
			plugin.PluginInterface.SavePluginConfig(this);
		}
	}
}