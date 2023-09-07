using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Ktisis.Common.Utility;
using Ktisis.ImGuizmo;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.Models;
using Ktisis.Scene.Objects.World;
using Ktisis.Scene.Editing.Modes;
using Ktisis.Scene.Impl;

namespace Ktisis.Scene.Editing;

[Flags]
public enum EditFlags {
	None = 0,
	Propagate = 1,
	Mirror = 2
}

public enum EditMode {
	None = 0,
	Object = 1,
	Pose = 2
}

public class SceneEditor {
	// Constructor

	private readonly SceneManager Manager;

	public readonly SelectState Selection;

	public SceneEditor(SceneManager manager) {
		this.Manager = manager;
		this.Selection = new SelectState();

		this.AddMode<PoseMode>(EditMode.Pose)
			.AddMode<ObjectMode>(EditMode.Object);

		manager.OnSceneChanged += OnSceneChanged;
	}

	// Editor state

	public EditFlags Flags = EditFlags.Propagate;

	public EditMode CurrentMode = EditMode.Object;

	public Mode TransformMode = Mode.Local;
	public Operation TransformOp = Operation.ROTATE;

	// Register mode handlers

	private readonly Dictionary<EditMode, ModeHandler> Modes = new();

	private SceneEditor AddMode<T>(EditMode id) where T : ModeHandler {
		var inst = (T)Activator.CreateInstance(typeof(T), this.Manager)!;
		this.Modes.Add(id, inst);
		return this;
	}

	// Access mode handlers

	public ModeHandler? GetHandler() => this.CurrentMode switch {
		EditMode.None => null,
		var key => this.Modes[key]
	};

	public IReadOnlyDictionary<EditMode, ModeHandler> GetHandlers()
		=> this.Modes.AsReadOnly();

	// Events

	private void OnSceneChanged(SceneGraph? scene) {
		if (scene is not null)
			this.Selection.Attach(scene);
		else
			this.Selection.Clear();
	}

	// Objects

	public bool IsItemInfluenced(SceneObject item) {
		var mode = this.CurrentMode;
		return item switch {
			ArmatureNode => mode is EditMode.Pose,
			WorldObject => mode is EditMode.Object,
			_ => true
		};
	}

	public ITransform? GetTransformTarget()
		=> GetHandler()?.GetTransformTarget();

	public Transform? GetTransform()
		=> GetTransformTarget()?.GetTransform();

	public void Manipulate(ITransform target, Matrix4x4 targetMx, Matrix4x4 deltaMx) {
		foreach (var (_, handler) in GetHandlers())
			handler.Manipulate(target, targetMx, deltaMx);
	}
}
