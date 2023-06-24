using Dalamud.Logging;

using Ktisis.Core.Singletons;
using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Providers;

namespace Ktisis.Scene; 

public class SceneManager : Singleton, IEventClient {
	// Scene
	
	public static Scene? Scene { get; private set; }
	
	// GPose Event

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
}