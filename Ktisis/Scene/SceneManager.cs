using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Editor.Context.Types;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Factory.Types;
using Ktisis.Scene.Modules;
using Ktisis.Scene.Modules.Actors;
using Ktisis.Scene.Modules.Lights;
using Ktisis.Scene.Types;

namespace Ktisis.Scene;

public class SceneManager : SceneModuleContainer, ISceneManager {
	public bool IsValid => this.Context.IsValid && !this.IsDisposing;
	
	public IEditorContext Context { get; }
	public IEntityFactory Factory { get; }

	private readonly SceneRoot Root;
	
	public SceneManager(
		IEditorContext context,
		HookScope scope,
		IEntityFactory factory
	) : base(scope) {
		this.Context = context;
		this.Factory = factory;
		this.Root = new SceneRoot(this);
	}
	
	// Initialization

	public void Initialize() {
		Ktisis.Log.Info("Initializing scene...");
		this.SetupModules();
	}
	
	private void SetupModules() {
		var gpose = this.AddModule<GroupPoseModule>();
		this.AddModule<ActorModule>(gpose);
		this.AddModule<LightModule>(gpose);
		this.AddModule<EnvModule>();
		this.InitializeModules();
	}
	
	// Update handler
	
	public double UpdateTime { get; private set; }

	public void Update() {
		if (!this.IsValid) return;
		var t = new Stopwatch();
		t.Start();
		this.UpdateModules();
		this.Root.Update();
		t.Stop();
		this.UpdateTime = t.Elapsed.TotalMilliseconds;
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
	
	// Utility
	
	public ActorEntity? GetEntityForActor(GameObject actor) => this.Children.ToList()
		.Where(entity => entity is ActorEntity { IsValid: true })
		.Cast<ActorEntity>()
		.FirstOrDefault(entity => entity.Actor.ObjectIndex == actor.ObjectIndex);
	
	// Disposal

	private bool IsDisposing;
    
	public void Dispose() {
		this.IsDisposing = true;
		try {
			this.Root.Clear();
			this.DisposeModules();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to dispose scene!\n{err}");
		}
		GC.SuppressFinalize(this);
	}
}
