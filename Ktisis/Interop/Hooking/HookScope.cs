using System;
using System.Collections.Generic;

namespace Ktisis.Interop.Hooking;

public class HookScope : IHookModule {
	private readonly IHookMediator _hook;
	
	private readonly List<HookModule> Modules = new();

	public HookScope(
		IHookMediator hook
	) {
		this._hook = hook;
	}

	private bool _init;
	public bool IsInit => this._init;
	
	// Hook access
	
	public void EnableAll()
		=> this.Modules.ForEach(mod => mod.EnableAll());
	
	public void DisableAll()
		=> this.Modules.ForEach(mod => mod.DisableAll());
	
	public void SetEnabled(bool enabled) {
		if (enabled)
			this.EnableAll();
		else
			this.DisableAll();
	}

	public bool TryGetHook<T>(out HookWrapper<T>? result) where T : Delegate {
		foreach (var module in this.Modules) {
			if (!module.TryGetHook<T>(out var hook))
				continue;
			result = hook;
			return true;
		}
		result = null;
		return false;
	}
	
	// Modules

	public T Create<T>(params object[] param) where T : HookModule {
		var module = this._hook.Create<T>(param);
		this.Modules.Add(module);
		return module;
	}
	
	public bool Initialize() {
		var result = false;
		foreach (var module in this.Modules)
			result |= module.Initialize();
		return this._init = result;
	}
	
	// Disposal
	
	public void Dispose() {
		this.Modules.ForEach(mod => mod.Dispose());
		this.Modules.Clear();
	}
}
