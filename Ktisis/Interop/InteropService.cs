using System;

using Dalamud.Game;

using Ktisis.Interop.Hooking;
using Ktisis.Interop.Unmanaged;

namespace Ktisis.Interop; 

public class InteropService : IDisposable {
	// Service

	private readonly DllResolver DllResolver;
	
	public readonly HookManager Hooks;

	public InteropService(ISigScanner _sig) {
		this.DllResolver = new DllResolver();
		this.Hooks = new HookManager(_sig);
	}
	
	// Disposal

	private bool IsDisposed;
	
	public void Dispose() {
		if (this.IsDisposed) return;
		this.IsDisposed = true;
		this.Hooks.Dispose();
		this.DllResolver.Dispose();
	}
}