using System;
using System.Linq;
using System.Collections.Generic;

using Ktisis.Core.Providers;

namespace Ktisis.Interop.Hooking;

[Flags]
internal enum HookFlags {
	None = 0,
	Automatic = 1,
	RequireEvent = 2,
	HardDisable = 4
}

internal class HookManager {
	// Modules

	private readonly List<HookModule> Modules = new();

	// Module and hook creation

	internal T Register<T>(Condition cond = Condition.Any, HookFlags flags = HookFlags.None) where T : HookModule, new() {
		var test = new T();

		test.Condition = cond;
		if (flags.HasFlag(HookFlags.RequireEvent))
			flags |= HookFlags.HardDisable;
		test.Flags = flags;

		Modules.Add(test);
		return test;
	}

	internal T? GetModule<T>() where T : HookModule
		=> (T?)Modules.FirstOrDefault(m => m is T, null);

	internal void CreateHooks() => Modules.ForEach(m => m.Create());

	// Conditions

	internal void OnEventAdded() {
		foreach (var module in Modules.Where(m => m.Flags.HasFlag(HookFlags.RequireEvent)))
			module.Flags &= ~HookFlags.HardDisable;
	}

	internal void OnConditionUpdate(Condition cond, bool value) {
		foreach (var module in Modules.Where(m => m.Condition.HasFlag(cond))) {
			if (!value)
				module.DisableHooks();
			else if (module.Flags.HasFlag(HookFlags.Automatic))
				module.EnableHooks();
		}
	}

	// Disposal

	internal void Dispose() => Modules.ForEach(m => m.Dispose());
}
