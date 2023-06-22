using System;
using System.Collections.Generic;

using Ktisis.Core;

namespace Ktisis.Interop.Hooking;

public abstract class HookModule : IDisposable {
	private readonly List<FuncHook> Hooks = new();

	public abstract void Create();

	public void Dispose() => Hooks.ForEach(h => h.Dispose());

	// Hook creation

	protected FuncHook<T> GetSignature<T>(T detour, string sig) where T : Delegate {
		var address = Services.SigScanner.ScanText(sig);
		var hook = new FuncHook<T>(address, detour);
		Hooks.Add(hook);
		return hook;
	}

	// Hook helpers

	public void EnableAll() => Hooks.ForEach(h => h.Enable());
	public void DisableAll() => Hooks.ForEach(h => h.Disable());
}
