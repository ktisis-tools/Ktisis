using Dalamud.IoC;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Game.Gui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Objects;

using Ktisis.Data;
using Ktisis.Events;
using Ktisis.Interop;
using Ktisis.Core.Providers;
using Ktisis.Core.Singletons;

namespace Ktisis.Core;

public class Services : ServiceProvider {
	internal static bool Ready;
	protected override bool _Ready { get => Ready; set => Ready = value; }

	[PluginService] internal static GameGui GameGui { get; private set; } = null!;
	[PluginService] internal static Framework Framework { get; private set; } = null!;
	[PluginService] internal static SigScanner SigScanner { get; private set; } = null!;
	[PluginService] internal static ObjectTable ObjectTable { get; private set; } = null!;
	[PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
	[PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;

	[Service(ServiceFlags.Critical)] internal static InteropService Interop = new();

	[Service] internal static DataService Data = new();
	[Service] internal static EventRouter Events = new();
	[Service] internal static GPoseService GPose = new();
	[Service] internal static ConditionService Conditions = new();

	protected override void OnInitService(Service service) {
		if (service is IEventClient eventClient)
			Events.Create(eventClient);
	}

	protected override void OnDisposeService(Service service) {
		if (service is IEventClient eventClient)
			Events.Remove(eventClient);
	}
}
