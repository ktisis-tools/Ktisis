using System;
using System.Threading.Tasks;

using Dalamud.Interface.Internal.Notifications;

using JetBrains.Annotations;

using Dalamud.Logging;

using Ktisis.Core;
using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Core.Singletons;
using Ktisis.Providers;

namespace Ktisis.Scene; 

public class SceneManager : Singleton, IEventClient {
	// Scene

	public static Scene? Scene;

	// GPose Event

	[UsedImplicitly]
	[Listener<GPoseEvent>]
	public void OnEnterGPose(object sender, bool isActive) {
		if (isActive) {
			// Entering gpose
			PluginLog.Verbose("Entering gpose, setting up scene...");
			Scene = Scene.Create();
		} else {
			// Leaving gpose
			PluginLog.Verbose("Leaving gpose, cleaning up scene...");
			Scene = null;
		}
	}
}