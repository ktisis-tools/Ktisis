using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Ktisis.Editor.Context;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Modules;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Modules.Lights;
using Ktisis.Scene.Types;

namespace Ktisis.Scene;

public class SceneManager : ISceneManager {
	private readonly IContextMediator _mediator;
	private readonly HookScope _scope;
	
	private readonly Dictionary<Type, SceneModule> Modules = new();

	private SceneRoot Root { get; set; } = null!;
	
	public IEntityFactory Factory { get; }
	
	public IEditorContext Context => this._mediator.Context;
	public bool IsValid => this.Context is { IsValid: true } && !this.IsDisposing;
	
	// Construction
	
	public SceneManager(
		IContextMediator mediator,
		HookScope scope,
		IEntityFactory factory
	) {
		this._mediator = mediator;
		this._scope = scope;
		this.Factory = factory;
	}
	
	// Modules
	
	public T GetModule<T>() where T : SceneModule
		=> (T)this.Modules[typeof(T)];

	public bool TryGetModule<T>(out T? module) where T : SceneModule {
		module = null;
		var result = this.Modules.TryGetValue(typeof(T), out var value);
		if (result) module = value as T;
		return result;
	}

	public SceneManager SetupModules() {
		var gpose = this.AddModuleAndGet<GroupPoseModule>();
		return this.AddModule<ActorModule>(gpose)
			.AddModule<LightModule>(gpose)
			.AddModule<EnvModule>();
	}

	private SceneManager AddModule<T>(params object[] param) where T : SceneModule {
		this.AddModuleAndGet<T>(param);
		return this;
	}

	private T AddModuleAndGet<T>(params object[] param) where T : SceneModule {
		var module = this._scope.Create<T>(param.Prepend(this).ToArray());
		this.Modules.Add(typeof(T), module);
		return module;
	}
	
	// Scene setup & events

	public double UpdateTime { get; private set; }

	public void Initialize() {
		Ktisis.Log.Info("Initializing scene...");
		
		this.Root = new SceneRoot(this);

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

	public void Update() {
		if (!this.IsValid) return;
		var t = new Stopwatch();
		t.Start();
		foreach (var module in this.Modules.Values)
			RunModuleUpdate(module);
		this.Root.Update();
		t.Stop();
		this.UpdateTime = t.Elapsed.TotalMilliseconds;
	}

	private static void RunModuleUpdate(SceneModule module) {
		try {
			module.Update();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to update module '{module.GetType().Name}':\n{err}");
		}
	}
	
	// Refresh

	public void Refresh() {
		var entities = this.Root.Recurse()
			.Where(entity => entity is IConfigurable)
			.Cast<IConfigurable>();
		
		foreach (var entity in entities)
			entity.Refresh();
	}
	
	// Objects

	public SceneEntity? Parent {
		get => this.Root.Parent;
		set => this.Root.Parent = value;
	}
	
	public IEnumerable<SceneEntity> Children => this.Root.Children;

	public bool Add(SceneEntity entity) => this.Root.Add(entity);
	public bool Remove(SceneEntity entity) => this.Root.Remove(entity);

	public IEnumerable<SceneEntity> Recurse() => this.Root.Recurse();
	
	// Disposal

	private bool IsDisposing;
    
	public void Dispose() {
		this.IsDisposing = true;
		try {
			foreach (var module in this.Modules.Values)
				module.Dispose();
			this.Modules.Clear();
			this.Root.Clear();
			this.Root = null!;
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to dispose scene!\n{err}");
		}
		GC.SuppressFinalize(this);
	}
}
