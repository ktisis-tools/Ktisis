using System;
using System.Collections.Generic;

using Dalamud.Game;
using Dalamud.Logging;

using Ktisis.Core;
using Ktisis.Services;
using Ktisis.Scene.Editing;
using Ktisis.Scene.Handlers;

namespace Ktisis.Scene;

public delegate void SceneChangedHandler(SceneGraph? scene);

public class SceneManager : IDisposable {
	// Service
    
	private readonly Framework _framework;
	private readonly GPoseService _gpose;
	private readonly IServiceContainer _services;

	private readonly SceneContext Context;
	
	public readonly SceneEditor Editor;

	public SceneManager(Framework _framework, GPoseService _gpose, IServiceContainer _services) {
		this._services = _services;
		this._framework = _framework;
		this._gpose = _gpose;

		this.Context = _services.Inject<SceneContext>(this);

		this.Editor = new SceneEditor(this);
		this.AddHandler<ActorHandler>()
			.AddHandler<LightHandler>()
			.AddHandler<ObjectHandler>();
		
		_framework.Update += OnFrameworkUpdate;
		_gpose.OnGPoseUpdate += OnGPoseUpdate;
	}
	
	// Scene state

	public bool IsActive => this.Scene is not null;
	
	public SceneGraph? Scene { get; private set; }
	
	// Object managers
	
	private readonly Dictionary<Type, object> ObjectHandlers = new();

	private SceneManager AddHandler<T>() {
		var inst = this._services.Inject<T>(this)!;
		this.ObjectHandlers.Add(typeof(T), inst);
		return this;
	}

	public T GetHandler<T>() {
		this.ObjectHandlers.TryGetValue(typeof(T), out var manager);
		if (manager is null)
			throw new Exception($"Failed to retrieve object manager: {typeof(T)}");
		return (T)manager;
	}
	
	// Events

	public event SceneChangedHandler? OnSceneChanged;

	private void OnFrameworkUpdate(object _sender) {
		if (this.IsDisposed) return;
		
		if (this._gpose.IsInGPose)
			this.Scene?.Update();
	}

	private void OnGPoseUpdate(bool active) {
		if (this.IsDisposed) return;
		
		if (active) {
			PluginLog.Verbose("Entering gpose, setting up scene...");
			this.Scene = new SceneGraph(this.Context);
		} else {
			PluginLog.Verbose("Leaving gpose, cleaning up scene...");
			this.Scene = null;
		}

		this.OnSceneChanged?.Invoke(this.Scene);
	}
	
	// Disposal

	private bool IsDisposed;
    
	public void Dispose() {
		if (this.IsDisposed) return;
		this.IsDisposed = true;
		
		this._framework.Update -= OnFrameworkUpdate;
		this._gpose.OnGPoseUpdate -= OnGPoseUpdate;
		
		this.Scene = null;
		this.ObjectHandlers.Clear();
	}
}
