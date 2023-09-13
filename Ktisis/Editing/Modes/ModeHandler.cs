using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

using Ktisis.Editing.Attributes;
using Ktisis.Scene;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;

namespace Ktisis.Editing.Modes;

public abstract class ModeHandler {
	// Constructor

	protected readonly SceneManager Manager;
	protected readonly EditorService Editor;

	protected ModeHandler(SceneManager mgr, EditorService editor) {
		this.Manager = mgr;
		this.Editor = editor;
	}

	// Enumeration

	public abstract IEnumerable<SceneObject> GetEnumerator();

	// Transforms

	public abstract ITransform? GetTransformTarget(IEnumerable<SceneObject> objects);

	public abstract void Manipulate(ITransform target, Matrix4x4 final, Matrix4x4 initial, IEnumerable<SceneObject> objects);
	
	// Attribute access

	public ObjectModeAttribute? GetAttribute() => this.GetType()
		.GetCustomAttribute<ObjectModeAttribute>();

	public Type? GetRenderer() => this.GetAttribute()?.Renderer;
}
