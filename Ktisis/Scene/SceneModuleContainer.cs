using System;
using System.Collections.Generic;
using System.Linq;

using Ktisis.Interop.Hooking;
using Ktisis.Scene.Modules;

namespace Ktisis.Scene;

public class SceneModuleContainer {
	private readonly HookScope _scope;

	public SceneModuleContainer(HookScope scope) {
		this._scope = scope;
	}
	
	// Modules
	
	private readonly Dictionary<Type, SceneModule> Modules = new();
	
	public T GetModule<T>() where T : SceneModule => (T)this.Modules[typeof(T)];

	public bool TryGetModule<T>(out T? module) where T : SceneModule {
		module = null;
		var result = this.Modules.TryGetValue(typeof(T), out var value);
		if (result) module = value as T;
		return result;
	}

	protected T AddModule<T>(params object[] param) where T : SceneModule {
		var module = this._scope.Create<T>(param.Prepend(this).ToArray());
		this.Modules.Add(typeof(T), module);
		return module;
	}
	
	// Initialization

	protected void InitializeModules() {
		var init = this.Modules.Values
			.Where(module => module.Initialize() && module.IsInit);

		foreach (var module in init) {
			try {
				module.Setup();
			} catch (Exception err) {
				Ktisis.Log.Error($"Failed to setup module '{module.GetType().Name}':\n{err}");
			}
		}
	}
	
	// Update modules

	protected void UpdateModules() {
		foreach (var (type, module) in this.Modules) {
			try {
				module.Update();
			} catch (Exception err) {
				Ktisis.Log.Error($"Failed to handle update for module '{type.Name}':\n{err}");
			}
		}
	}
	
	// Disposal

	protected void DisposeModules() {
		foreach (var module in this.Modules.Values)
			module.Dispose();
		this.Modules.Clear();
	}
}
