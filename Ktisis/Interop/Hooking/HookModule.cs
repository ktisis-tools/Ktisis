using System;
using System.Collections.Generic;

using Dalamud.Logging;

using Ktisis.Core;
using Ktisis.Events;

namespace Ktisis.Interop.Hooking;

internal abstract class HookModule : IDisposable {
	private readonly List<FuncHook> Hooks = new();

	internal Condition Condition;
	internal HookFlags Flags;

	internal abstract void Create();

	// Hook creation

	protected FuncHook<T> GetSignature<T>(T detour, string sig) where T : Delegate {
		var address = Services.SigScanner.ScanText(sig);
		var hook = new FuncHook<T>(address, detour);
		Hooks.Add(hook);
		return hook;
	}

	// Hook helpers

	internal bool Enabled { get; private set; }

	internal bool EnableHooks() {
		PluginLog.Verbose($"Enabling all hooks for module {GetType().Name}");
		if (Flags.HasFlag(HookFlags.HardDisable) || Condition != Condition.Any && !Services.Conditions.Check(Condition)) {
			PluginLog.Warning("Attempted to enable module, but condition checks failed!");
			return false;
		}
		Hooks.ForEach(h => h.Enable());
		return Enabled = true;
	}

	internal void DisableHooks() {
		Hooks.ForEach(h => h.Disable());
		Enabled = false;
	}

	// Disposal

	public void Dispose() {
		Hooks.ForEach(h => h.Dispose());
		Hooks.Clear();
	}
}
