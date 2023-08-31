using System;
using System.Collections.Generic;

using Ktisis.Core;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Handlers;
using Ktisis.Common.Extensions;

namespace Ktisis.Scene;

public delegate void SceneEventHandler(SceneGraph sender);
public delegate void SceneObjectEventHandler(SceneGraph sender, SceneObject item);

public class SceneGraph : IParentable<SceneObject> {
	// Constructor

	private readonly IServiceContainer _services;

	private readonly SceneContext Context;

	public readonly SelectState Select;

	public SceneGraph(IServiceContainer _services) {
		this._services = _services;

		this.Context = _services.Inject<SceneContext>(this);
		this.AddHandler<ActorHandler>()
			.AddHandler<LightHandler>()
			.AddHandler<ObjectHandler>();

		this.Select = new SelectState(this);
	}

	// Events

	public event SceneEventHandler? OnSceneBuild;
	public event SceneEventHandler? OnSceneUpdate;

	public event SceneObjectEventHandler? OnSceneObjectRemoved;

	// Managers
	// TODO: Move this to SceneManager?

	private readonly Dictionary<Type, object> ObjectHandlers = new();

	private SceneGraph AddHandler<T>() {
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

	// Build scene

	public void Build() {
		this.OnSceneBuild?.InvokeSafely(this);
	}

	// Tick update

	public void Update() {
		this.OnSceneUpdate?.InvokeSafely(this);
		this.Objects.ForEach(obj => obj.Update(this.Context));
	}

	// Object management

	private readonly List<SceneObject> Objects = new();

	public void Remove(SceneObject item) {
		item.Flags |= ObjectFlags.Removed;
		if (item.Parent is null)
			this.Objects.Remove(item);
		else
			item.SetParent(null);

		foreach (var child in item.GetChildren())
			Remove(child);

		this.OnSceneObjectRemoved?.InvokeSafely(this, item);
	}

	// IParentable

	public int Count => this.Objects.Count;

	public void AddChild(SceneObject child) {
		this.Objects.Add(child);
	}

	public void RemoveChild(SceneObject child) {
		this.Objects.Remove(child);
	}

	public IReadOnlyList<SceneObject> GetChildren()
		=> this.Objects.AsReadOnly();
}
