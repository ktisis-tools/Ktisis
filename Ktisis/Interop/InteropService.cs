using Dalamud.Logging;

using Ktisis.Core.Singletons;
using Ktisis.Interop.Hooking;
using Ktisis.Interop.Modules;

namespace Ktisis.Interop; 

public class InteropService : Service {
	// Hooking

	private readonly HookManager HookManager = new();

	// Initialization
	
	public override void Init() {
		PluginLog.Information($"InteropService Init");
		HookManager.Register<PoseHooks>();
		HookManager.CreateHooks();
	}
	
	// Dispose

	public override void Dispose() {
		PluginLog.Information($"InteropService Dispose");
		HookManager.Dispose();
	}
}