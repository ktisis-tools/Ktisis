using System.Collections.Generic;

namespace Ktisis.Interop.Hooking; 

internal class HookManager {
	// Modules

	private readonly List<HookModule> Modules = new();

	// Module creation and disposal
	
	internal void Register<T>() where T : HookModule, new() {
		var test = new T();
		Modules.Add(test);
	}

	internal void CreateHooks() => Modules.ForEach(m => m.Create());

	internal void Dispose() => Modules.ForEach(m => m.Dispose());
}