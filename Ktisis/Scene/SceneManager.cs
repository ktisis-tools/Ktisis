using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Ktisis.Editor.Context;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Factory;
using Ktisis.Scene.Modules;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Modules.Lights;
using Ktisis.Scene.Types;

namespace Ktisis.Scene;

public interface ISceneManager : IComposite, IDisposable {
	public bool IsValid { get; }
	
	public IEditorContext Context { get; }
	
	public IEntityFactory Factory { get; }

	public T GetModule<T>() where T : SceneModule;
	public bool TryGetModule<T>(out T? module) where T : SceneModule;
	
	public double UpdateTime { get; }

	public void Initialize();
	public void Update();
}

public class SceneManager : ISceneManager {
	private readonly IContextMediator _mediator;
	private readonly HookScope _scope;
	
	private readonly SceneRoot Root;
	private readonly Dictionary<Type, SceneModule> Modules = new();
	
	public IEntityFactory Factory { get; init; }
	
	public IEditorContext Context => this._mediator.Context;
	public bool IsValid => this.Context is { IsValid: true } && !this.IsDisposing;
	
	// Construction
	
	public SceneManager(
		IContextMediator mediator,
		HookScope scope
	) {
		this._mediator = mediator;
		this._scope = scope;
		this.Root = new SceneRoot(this);
		this.Factory = new EntityFactory(this);
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
		return this.AddModule<ActorModule>()
			.AddModule<EnvModule>()
			.AddModule<LightModule>()
			.AddModule<PoseModule>();
	}

	private SceneManager AddModule<T>() where T : SceneModule {
		var module = this._scope.Create<T>(this);
		this.Modules.Add(typeof(T), module);
		return this;
	}
	
	// Scene setup & events

	public double UpdateTime { get; private set; } = 0.0f;

	public void Initialize() {
		Ktisis.Log.Info("Initializing scene...");

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
		foreach (var module in this.Modules.Values)
			module.Dispose();
		this.Modules.Clear();
		this.Root.Clear();
		GC.SuppressFinalize(this);
	}
}
