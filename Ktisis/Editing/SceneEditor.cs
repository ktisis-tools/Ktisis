using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Ktisis.Scene;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.Models;
using Ktisis.Scene.Objects.World;
using Ktisis.Common.Utility;
using Ktisis.Editing.Modes;
using Ktisis.Data.Config;
using Ktisis.Core.Impl;

namespace Ktisis.Editing;

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

[KtisisService]
public class SceneEditor : IServiceInit {
	// Constructor

	private readonly ConfigService _cfg;
	private readonly SceneManager _scene;
	
	public readonly SelectState Selection;

	private ConfigFile Config => this._cfg.Config;
	
	public SceneEditor(SceneManager _scene, ConfigService _cfg) {
		this._cfg = _cfg;
		this._scene = _scene;
		
		this.Selection = new SelectState();
		this.Selection.OnSelectionChanged += OnSelectionChanged;

		this.AddMode<PoseMode>(EditMode.Pose)
			.AddMode<ObjectMode>(EditMode.Object);
		
		_scene.OnSceneChanged += OnSceneChanged;
	}

	// Editor state

	private EditMode CurrentMode => this._cfg.Config.Editor_Mode;

	// Register mode handlers

	private readonly Dictionary<EditMode, ModeHandler> Modes = new();

	private SceneEditor AddMode<T>(EditMode id) where T : ModeHandler {
		var inst = (T)Activator.CreateInstance(typeof(T), this._scene, this, this._cfg)!;
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

	private void OnSelectionChanged(SelectState state) {
		if (state.Count != 1) return;
		
		var mode = this.Config.Editor_Mode;
		this.Config.Editor_Mode = state.GetSelected().LastOrDefault() switch {
			ArmatureNode => EditMode.Pose,
			SceneObject => EditMode.Object,
			_ => mode
		};
	}

	// Objects

	public bool IsItemInfluenced(SceneObject item) {
		var mode = this.Config.Editor_Mode;
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
