using Dalamud.IoC;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Game.Gui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;

namespace Ktisis {
	internal class Dalamud {
		[PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
		[PluginService] internal static DataManager DataManager { get; private set; } = null!;
		[PluginService] internal static ClientState ClientState { get; private set; } = null!;
		[PluginService] internal static ObjectTable ObjectTable { get; private set; } = null!;
		[PluginService] internal static SigScanner SigScanner { get; private set; } = null!;
		[PluginService] internal static GameGui GameGui { get; private set; } = null!;

		public static void Init(DalamudPluginInterface dalamud) {
			dalamud.Create<Dalamud>();
		}
	}
}