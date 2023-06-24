using Ktisis.Core.Singletons;
using Ktisis.Interop.Hooking;
using Ktisis.Interop.Modules;

namespace Ktisis.Interop;

public class InteropService : Service {
	// Hooking

	private readonly HookManager HookManager = new();

	// Initialization

	public override void Init() {
		HookManager.Register<PoseHooks>();
		HookManager.CreateHooks();
	}

	// Dispose

	public override void Dispose() {
		HookManager.Dispose();
	}
}
