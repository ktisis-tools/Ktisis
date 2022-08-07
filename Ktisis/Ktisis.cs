using System;
using System.IO;
using System.Reflection;

using Dalamud;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.Command;

namespace Ktisis {
	public sealed class Ktisis : IDalamudPlugin {
		public string Name => "Ktisis";

		private DalamudPluginInterface PluginInterface { get; init; }
		private CommandManager CommandManager { get; init; }

		public Ktisis(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager cmdManager
		) {
			this.PluginInterface = pluginInterface;
			this.CommandManager = cmdManager;
		}

		public void Dispose() {

		}
	}
}
