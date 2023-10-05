using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

using Ktisis.Scene;
using Ktisis.Scene.Impl;
using Ktisis.Scene.Objects;
using Ktisis.Scene.Objects.Skeleton;
using Ktisis.Scene.Objects.World;
using Ktisis.Common.Utility;
using Ktisis.Editing.Modes;
using Ktisis.Core;
using Ktisis.Data.Config;
using Ktisis.Editing.History;
using Ktisis.Editing.History.Actions;
using Ktisis.Editing.History.Clients;
using Ktisis.Events;

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

// TODO: Consider splitting this to TransformService?

[DIService]
public class EditorService {
	// Constructor

	private readonly ConfigService _cfg;
	private readonly SceneManager _scene;
	private readonly HistoryService _history;
	
	public readonly SelectState Selection;

	private TransformHistory History = null!;

	private ConfigFile Config => this._cfg.Config;
	
	public EditorService(
		ConfigService _cfg,
		SceneManager _scene,
		HistoryService _history,
		InitEvent _init
	) {
		this._cfg = _cfg;
		this._scene = _scene;
		this._history = _history;
		
		this.Selection = new SelectState();
		this.Selection.OnSelectionChanged += OnSelectionChanged;

		this.AddMode<PoseMode>(EditMode.Pose)
			.AddMode<ObjectMode>(EditMode.Object);

		_init.Subscribe(Initialize);
	}

	private void Initialize() {
		this.History = this._history.CreateClient<TransformHistory>("SceneEditor_Transform");
		this.History.AddHandler(this.OnUndoRedo);
        
		this._scene.OnSceneChanged += this.Selection.Update;
	}

	// Editor state
	
	private EditMode CurrentMode => this._cfg.Config.Editor_Mode;
	
	// Register mode handlers

	private readonly Dictionary<EditMode, ModeHandler> Modes = new();

	private EditorService AddMode<T>(EditMode id) where T : ModeHandler {
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
	
	// Objects

	public bool IsItemInfluenced(SceneObject item) {
		var mode = this.Config.Editor_Mode;
		return item switch {
			ArmatureNode => mode is EditMode.Pose,
			WorldObject => mode is EditMode.Object,
			_ => true
		};
	}
	
	// Transforms
	// TODO: Explicit handling for world/local transforms

	public ITransform? GetTransformTarget() {
		var select = this.Selection.GetSelected();
		return GetHandler()?.GetTransformTarget(select);
	}

	public Transform? GetTransform()
		=> GetTransformTarget()?.GetTransform();

	public Matrix4x4? GetTransformMatrix()
		=> GetTransformTarget()?.GetMatrix();

	public void Manipulate(ITransform target, Matrix4x4 targetMx) {
		var select = this.Selection.GetSelected().ToList();
		if (target is SceneObject sceneObj && this.History.UpdateOrBegin(sceneObj, targetMx))
			this.History.AddSubjects(select);

		Manipulate(select, target, targetMx);
	}

	private void Manipulate(IEnumerable<SceneObject> _objects, ITransform target, Matrix4x4 targetMx) {
		var initial = target.GetMatrix();
		if (initial == null) return;
		
		var objects = _objects.ToList();
		foreach (var (_, handler) in GetHandlers())
			handler.Manipulate(target, targetMx, initial.Value, objects);
	}

	public void EndTransform() {
		this.History.End();
	}
	
	// Events

	private void OnSelectionChanged(SelectState state, SceneObject? item) {
		if (state.Count != 1) return;
		
		var mode = this.Config.Editor_Mode;
		this.Config.Editor_Mode = state.GetSelected().LastOrDefault() switch {
			ArmatureNode => EditMode.Pose,
			SceneObject => EditMode.Object,
			_ => mode
		};

		if (item is IDummy dummy && GetTransformTarget() == item)
			dummy.CalcTransform();
	}

	private bool OnUndoRedo(TransformAction action, HistoryMod mod) {
		if (action.TargetId == null ) return false;
		
		if (this._scene.Scene is not SceneGraph scene) return false;
		
		var subjects = new List<SceneObject>();
		ITransform? target = null;
		
		foreach (var item in scene.RecurseChildren()) {
			if (item is not ITransform transform) continue;
			
			if (action.SubjectIds.Contains(item.UiId))
				subjects.Add(item);

			if (item.UiId == action.TargetId)
				target = transform;
		}

		if (target == null) return false;

		switch (mod) {
			case HistoryMod.Undo when action.Initial != null:
				Manipulate(subjects, target, action.Initial.Value);
				break;
			case HistoryMod.Redo when action.Final != null:
				Manipulate(subjects, target, action.Final.Value);
				break;
			default:
				return false;
		}
		
		return true;
	}
}
