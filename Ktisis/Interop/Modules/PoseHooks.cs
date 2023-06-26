using Ktisis.Interop.Hooking;

namespace Ktisis.Interop.Modules;

internal class PoseHooks : HookModule {
	// Module
	
	internal void SetPosing(bool active) {
		if (Enabled == active) return;
		if (active)
			EnableHooks();
		else
			DisableHooks();
	}

	// Hooks

	internal override void Create() { }
}