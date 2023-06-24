using Ktisis.Core.Singletons;
using Ktisis.Events;
using Ktisis.Events.Attributes;
using Ktisis.Providers;

namespace Ktisis.Scene; 

public class SceneManager : Singleton, IEventClient {
	// Singleton
	
	public static Scene? Scene { get; private set; }
	
	// GPose Event

	[Listener<GPoseEvent>]
	public void OnEnterGPose(object sender, bool isActive) {
		// TODO
	}
}