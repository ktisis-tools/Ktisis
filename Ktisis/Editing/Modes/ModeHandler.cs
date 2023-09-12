using System;
using System.Numerics;
using System.Reflection;
using System.Collections.Generic;

using Ktisis.Editing.Attributes;
using Ktisis.Scene;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;

namespace Ktisis.Editing.Modes;

public abstract class ModeHandler {
	// Constructor
	
	protected readonly SceneManager Manager;
	protected readonly SceneEditor Editor;

	protected ModeHandler(SceneManager mgr, SceneEditor editor) {
		this.Manager = mgr;
		this.Editor = editor;
	}
	
	// Enumeration

	public abstract IEnumerable<SceneObject> GetEnumerator();
	
	// Transforms

	public abstract ITransform? GetTransformTarget();

	public abstract void Manipulate(ITransform target, Matrix4x4 matrix);
	
	// Attribute access

	public ObjectModeAttribute? GetAttribute()
		=> this.GetType().GetCustomAttribute<ObjectModeAttribute>();

	public Type? GetRenderer() => this.GetAttribute()?.Renderer;
}
