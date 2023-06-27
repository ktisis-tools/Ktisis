using System.Diagnostics;
using System.Threading.Tasks;

using Dalamud.Plugin;
using Dalamud.Logging;
using Dalamud.Interface.Internal.Notifications;

using Ktisis.Core;
using Ktisis.Core.Singletons;
using Ktisis.Interface;
using Ktisis.Scenes;

namespace Ktisis;

public sealed class Ktisis : IDalamudPlugin {
	// Plugin info

	public string Name => "Ktisis";
	
	// Plugin framework

	internal readonly static SingletonManager Singletons = new();

	private Task? InitTask;

	// Ctor called on plugin load
	
	public Ktisis(DalamudPluginInterface plugin) {
		/* Start plugin initialization asynchronously.
		 * This is generally intended to handle the *creation* of classes in the framework,
		 * such as registering dependencies, setting up event listeners, etc.
		 * Activation of these classes should not be done until this is complete! */

		InitTask = Init(plugin).ContinueWith(task => {
			if (task.Exception == null) return;

			PluginLog.Fatal("Ktisis failed to load due to the following error(s):");
			foreach (var err in task.Exception.InnerExceptions)
				PluginLog.Error(err.ToString());
			
			plugin.UiBuilder.AddNotification(
				"Ktisis failed to load. Please check your error log for more information.",
				"Ktisis", NotificationType.Error
			);

			InitTask = null;
		});
	}
	
	// Initialize singletons & services

	private async Task Init(DalamudPluginInterface api) {
		await Task.Yield();
		
		var timer = new Stopwatch();
		timer.Start();

		// Inject dalamud services
		api.Create<Services>();

		// Register singletons for initialization
		Singletons.Register<Services>();
		Singletons.Register<SceneManager>();
		Singletons.Register<Gui>();

		// Initialize registered singletons
		Singletons.Init();

		var initTime = timer.Elapsed.TotalMilliseconds;
		timer.Restart();

		// Invoke OnReady
		Singletons.OnReady();

		var readyTime = timer.Elapsed.TotalMilliseconds;
		timer.Stop();

		PluginLog.Verbose($"Plugin initialization complete.\n" +
			$" Init:   {initTime:0.00}ms\n" +
			$"Ready: + {readyTime:0.00}ms\n" +
			$"Total: = {initTime + readyTime:0.00}ms"
		);
	}

	// Dispose

	public void Dispose() {
		if (InitTask?.IsCompleted is false) {
			PluginLog.Warning("Dispose called while init in progress, waiting for completion...");
			InitTask?.Wait();
		}
		Singletons.Dispose();
	}
}