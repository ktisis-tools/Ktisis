using System;
using System.Numerics;
using System.Reflection;
using System.Collections.Generic;

using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Editing.Attributes;

namespace Ktisis.Scene.Editing.Modes;

public abstract class ModeHandler {
	// Constructor
	
	protected readonly SceneManager Manager;

	public ModeHandler(SceneManager mgr) {
		this.Manager = mgr;
	}
	
	// Enumeration

	public abstract IEnumerable<SceneObject> GetEnumerator();
	
	// Transforms

	public abstract ITransform? GetTransformTarget();

	public abstract void Manipulate(ITransform target, Matrix4x4 matrix, Matrix4x4 delta);
	
	// Attribute access

	public ObjectModeAttribute? GetAttribute()
		=> this.GetType().GetCustomAttribute<ObjectModeAttribute>();

	public Type? GetRenderer() => this.GetAttribute()?.Renderer;
}
