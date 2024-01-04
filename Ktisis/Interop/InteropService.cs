using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Plugin.Services;

using Ktisis.Core;
using Ktisis.Core.Attributes;
using Ktisis.Interop.Hooking;

namespace Ktisis.Interop;

[Singleton]
public class InteropService : IDisposable {
	private readonly DIBuilder _di;
	private readonly IGameInteropProvider _interop;

	private readonly List<HookModule> Modules = new();
	
	public InteropService(
		DIBuilder di,
		IGameInteropProvider interop
	) {
		this._di = di;
		this._interop = interop;
	}

	public T CreateModule<T>(params object[] param) where T : HookModule {
		var mediator = new HookMediator(this);
		var module = this._di.Create<T>(param.Append(mediator).ToArray());
		return module;
	}

	public HookScope CreateScope() {
		var mediator = new HookMediator(this);
		return new HookScope(mediator);
	}

	private bool InitModule(HookModule module) {
		if (module.IsInit) return true;

		bool result;
		try {
			this._interop.InitializeFromAttributes(module);
			result = true;
		} catch (Exception err) {
			result = false;
			Ktisis.Log.Error($"Failed to initialize module '{module.GetType().Name}'\n{err}");
		}
		return result;
	}

	private bool RemoveModule(HookModule module)
		=> !this.IsDisposing && this.Modules.Remove(module);
	
	// Mediator

	private class HookMediator : IHookMediator {
		private readonly InteropService _interop;

		private HookModule? Module;

		public bool IsValid => !this._interop.IsDisposing && this.Module is { IsInit: true };
		
		public HookMediator(
			InteropService interop
		) {
			this._interop = interop;
		}

		public T Create<T>(params object[] param) where T : HookModule
			=> this._interop.CreateModule<T>(param);

		public bool Init(HookModule module)
			=> this._interop.InitModule(module);

		public bool Remove(HookModule module)
			=> this._interop.RemoveModule(module);
	}
	
	// Disposal

	private bool IsDisposing;

	public void Dispose() {
		this.IsDisposing = true;
		this.Modules.ForEach(mod => mod.Dispose());
		this.Modules.Clear();
		GC.SuppressFinalize(this);
	}
}
