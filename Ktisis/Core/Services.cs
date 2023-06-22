using Dalamud.IoC;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Game.Gui;
using Dalamud.Game.Command;

using Ktisis.Interop;
using Ktisis.Core.Singletons;

namespace Ktisis.Core;

public class Services : ServiceProvider {
	[PluginService] internal static GameGui GameGui { get; private set; } = null!;
	[PluginService] internal static SigScanner SigScanner { get; private set; } = null!;
	[PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
	[PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;

	[Service(ServiceFlags.Critical)] internal static InteropService Interop = null!;
}