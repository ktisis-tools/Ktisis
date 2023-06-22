using System.Threading.Tasks;

using Dalamud.Plugin;
using Dalamud.Logging;
using Dalamud.Interface.Internal.Notifications;

using Ktisis.Core;
using Ktisis.Core.Singletons;
using Ktisis.Interface;

namespace Ktisis;

public sealed class Ktisis : IDalamudPlugin {
	// Plugin info

	public string Name => "Ktisis";

	// Plugin framework

	private readonly SingletonManager Singleton;

	// Ctor called on plugin load

	public Ktisis(DalamudPluginInterface plugin) {
		// Instantiate singleton manager
		Singleton = new SingletonManager();

		// Handle plugin initialization, triggering a dispose if it fails.

		Init(plugin).ContinueWith(task => {
			if (task.Exception == null) {
				OnReady();
				return;
			}

			PluginLog.Fatal("Ktisis failed to load due to the following error(s):");
			foreach (var err in task.Exception.InnerExceptions)
				PluginLog.Error(err.ToString());

			plugin.UiBuilder.AddNotification(
				"Ktisis failed to load. Please check your error log for more information.",
				"Ktisis", NotificationType.Error
			);
		});
	}

	// Initialize singletons & services

	private async Task Init(DalamudPluginInterface api) {
		await Task.Yield();

		// Inject dalamud services
		api.Create<Services>();

		// Register singletons for initialization
		Singleton.Register<Services>();
		Singleton.Register<Gui>();

		// Initialize registered singletons
		Singleton.Init();
	}

	// Code to run once all service initialization is complete

	private void OnReady() {
		PluginLog.Verbose("Ready");
	}

	// Dispose

	public void Dispose() => Singleton.Dispose();
}
