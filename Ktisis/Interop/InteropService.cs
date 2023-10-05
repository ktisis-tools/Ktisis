using System;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Plugin.Services;

using Ktisis.Core.Impl;
using Ktisis.Interop.Hooking;
using Ktisis.Interop.Unmanaged;

namespace Ktisis.Interop; 

[KtisisService]
public class InteropService : IServiceInit, IDisposable {
	// Service

	private readonly IFramework _frame;
	private readonly IGameInteropProvider _interop;

	private readonly HookManager Hooks;
	private readonly DllResolver DllResolver;
	
	public InteropService(
		IFramework _frame,
		ISigScanner _sig,
		IGameInteropProvider _interop
	) {
		this._frame = _frame;
		this._interop = _interop;
		
		this.Hooks = new HookManager(_sig, _interop);
		this.DllResolver = new DllResolver();
	}

	public void PreInit() {
		this.DllResolver.Create();
	}
	
	// Create hook containers
	
	public async Task<T> Create<T>() where T : HookContainer, new() {
		// This takes orders of magnitudes longer if not called from the framework thread.
		// Not completely sure why.
		var inst = await this._frame.RunOnFrameworkThread(() => {
			var inst = new T();
			this._interop.InitializeFromAttributes(inst);
			return inst;
		});

		inst.GetHooks().ForEach(this.Hooks.Add);

		return inst;
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
