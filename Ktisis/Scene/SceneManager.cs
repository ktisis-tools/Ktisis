using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

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
using Ktisis.Editor.Lights;
using Ktisis.Data.Files;
using Ktisis.Scene.Entities.World;

namespace Ktisis.Scene;

public class SceneManager : SceneModuleContainer, ISceneManager {
	public bool IsValid => this.Context.IsValid && !this.IsDisposing;
	
	public IEditorContext Context { get; }
	public IEntityFactory Factory { get; }
	private readonly IFramework _framework;

	private readonly SceneRoot Root;
	
	public SceneManager(
		IEditorContext context,
		HookScope scope,
		IFramework framework,
		IEntityFactory factory
	) : base(scope) {
		this.Context = context;
		this.Factory = factory;
		this.Root = new SceneRoot(this);
		this._framework = framework;
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
		this.SetupSavedState();
	}

	private void SetupSavedState() {
		foreach (var setup in this.Context.Config.Editor.ReferenceImages) {
			this.Factory.BuildRefImage()
				.FromData(setup)
				.Add();
		}
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
	
	public ActorEntity? GetEntityForActor(IGameObject actor) => this.GetEntityForIndex(actor.ObjectIndex);

	public ActorEntity? GetEntityForIndex(uint objectIndex) => this.Children.ToList()
		.Where(entity => entity is ActorEntity { IsValid: true })
		.Cast<ActorEntity>()
		.FirstOrDefault(entity => entity.Actor.ObjectIndex == objectIndex);

	public ActorEntity GetFirstActor() => this.Children
		.Where(entity => entity is ActorEntity { IsValid: true })
		.Cast<ActorEntity>()
		.OrderBy(entity => entity.Actor.ObjectIndex)
		.First();

	// Lights Utility (todo: should these live here longterm?)

	public Task ApplyLightFile(LightEntity light, LightFile file) {
		var converter = new EntityLightConverter(light);
		return this._framework.RunOnFrameworkThread(() => converter.Apply(file));
	}

	public Task<LightFile> SaveLightFile(LightEntity light) {
		var converter = new EntityLightConverter(light);
		return this._framework.RunOnFrameworkThread(() => converter.Save());
	}
	
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
