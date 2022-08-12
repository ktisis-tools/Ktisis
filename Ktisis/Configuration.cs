using System;

using Dalamud.Plugin;
using Dalamud.Configuration;

namespace Ktisis {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public int Version { get; set; } = 0;

		// Plugin settings & preferences

		public UserLocale Localization { get; set; } = UserLocale.En;

		// Interface

		public bool ShowOnEnterGpose { get; set; } = true;

		// Overlay

		public bool DrawLinesOnSkeleton { get; set; } = true;

		// Gizmo

		public bool AllowAxisFlip { get; set; } = true;

		// UI memory

		public bool ShowSkeleton { get; set; } = false;

		// save

		public void Save(Ktisis plugin) {
			plugin.PluginInterface.SavePluginConfig(this);
		}
	}

	public enum UserLocale {
		// these don't exist yet
		En = 0,
		De = 1,
		Jp = 2,
		Fr = 3
	}
}