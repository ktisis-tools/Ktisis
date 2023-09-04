using System.Collections.Generic;

using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;
using Ktisis.Common.Extensions;

namespace Ktisis.Scene;

public delegate void SceneEventHandler(SceneManager manager, SceneGraph sender);
public delegate void SceneObjectEventHandler(SceneGraph sender, SceneObject item);

public class SceneGraph : IParentable<SceneObject> {
	// Constructor
    
	private readonly SceneContext Context;

	public SceneGraph(SceneContext ctx) {
		this.Context = ctx;
	}
	
	// Events
    
	public event SceneEventHandler? OnSceneUpdate;

	public event SceneObjectEventHandler? OnSceneObjectRemoved;
	
	// Tick update

	public void Update(SceneManager manager) {
		this.OnSceneUpdate?.InvokeSafely(manager, this);
		this.Objects.ForEach(obj => obj.Update(manager, this.Context));
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