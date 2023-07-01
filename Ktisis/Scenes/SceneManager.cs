using System.Diagnostics;

using Dalamud.Game;

using JetBrains.Annotations;

using Dalamud.Logging;

using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Events.Providers;
using Ktisis.Core.Singletons;
using Ktisis.Core.Providers;

namespace Ktisis.Scenes; 

public class SceneManager : Singleton, IEventClient {
	// Scene

	public Scene? Scene;

	// GPose Event

	[UsedImplicitly]
	[Listener<GPoseEvent>]
	public void OnEnterGPose(object sender, bool isActive) {
		if (isActive) {
			// Entering gpose
			PluginLog.Verbose("Entering gpose, setting up scene...");
			Scene = new Scene();
		} else {
			// Leaving gpose
			PluginLog.Verbose("Leaving gpose, cleaning up scene...");
			Scene = null;
		}
	}

	[UsedImplicitly]
	[Listener<FrameworkEvent>]
	public void OnFrameworkUpdate(Framework _) {
		Scene?.Update();
	}
}