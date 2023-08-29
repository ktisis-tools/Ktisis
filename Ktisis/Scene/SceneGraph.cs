using System;
using System.Collections.Generic;

using Ktisis.Core;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Handlers;
using Ktisis.Scene.Objects;

namespace Ktisis.Scene;

public delegate void SceneEventDelegate(SceneGraph sender);

public class SceneGraph : IParentable<SceneObject> {
	// Constructor

	private readonly IServiceContainer _services;

	private readonly SceneContext Context;

	public SceneGraph(IServiceContainer _services) {
		this._services = _services;
		
		this.Context = _services.Inject<SceneContext>(this);
		this.AddHandler<ActorHandler>()
			.AddHandler<LightHandler>();
	}
	
	// Events

	public event SceneEventDelegate? OnSceneBuild;
	public event SceneEventDelegate? OnSceneUpdate;
	
	// Managers
	
	private readonly Dictionary<Type, object> ObjectHandlers = new();

	private SceneGraph AddHandler<T>() {
		var inst = this._services.Inject<T>(this)!;
		this.ObjectHandlers.Add(typeof(T), inst);
		return this;
	}

	private T GetHandler<T>() {
		this.ObjectHandlers.TryGetValue(typeof(T), out var manager);
		if (manager is null)
			throw new Exception($"Failed to retrieve object manager: {typeof(T)}");
		return (T)manager;
	}
	
	// Build scene

	public void Build() {
		this.OnSceneBuild?.Invoke(this);
	}
	
	// Tick update

	public void Update() {
		this.OnSceneUpdate?.Invoke(this);
		this.Objects.ForEach(obj => obj.Update(this.Context));
	}
	
	// Objects
	
	private readonly List<SceneObject> Objects = new();
	
	// Object management

	public void Remove(SceneObject obj) {
		obj.Flags |= ObjectFlags.Removed;
		if (obj.Parent is null)
			this.Objects.Remove(obj);
		else
			obj.SetParent(null);
	}
	
	// IParentable

	public int Count => this.Objects.Count;

	public void AddChild(SceneObject child) {
		this.Objects.Add(child);
	}

	public void RemoveChild(SceneObject child) {
		this.Objects.Remove(child);
	}

	public IReadOnlyList<SceneObject> GetChildren() => this.Objects.AsReadOnly();
}