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
using Ktisis.Editing.History;
using Ktisis.Editing.History.Actions;
using Ktisis.History;

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

[KtisisService]
public class EditorService : IServiceInit {
	// Constructor

	private readonly ConfigService _cfg;
	private readonly SceneManager _scene;
	
	public readonly SelectState Selection;

	private readonly TransformHistory History;

	private ConfigFile Config => this._cfg.Config;
	
	//private HistoryClient<>
	
	public EditorService(SceneManager _scene, HistoryService _history, ConfigService _cfg) {
		this._cfg = _cfg;
		this._scene = _scene;
		
		this.Selection = new SelectState();
		this.Selection.OnSelectionChanged += OnSelectionChanged;

		this.AddMode<PoseMode>(EditMode.Pose)
			.AddMode<ObjectMode>(EditMode.Object);
		
		_scene.OnSceneChanged += this.Selection.Update;

		this.History = _history.CreateClient<TransformHistory>("SceneEditor_Transform");
		this.History.AddHandler(this.OnUndoRedo);
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

	public ITransform? GetTransformTarget() {
		var select = this.Selection.GetSelected();
		return GetHandler()?.GetTransformTarget(select);
	}

	public Transform? GetTransform()
		=> GetTransformTarget()?.GetTransform();

	public void Manipulate(ITransform target, Matrix4x4 targetMx) {
		// TODO: This should pass selected objects into the handler.

		var select = this.Selection.GetSelected().ToList();

		if (target is SceneObject sceneObj && this.History.UpdateOrBegin(sceneObj, targetMx))
			this.History.AddSubjects(select);

		foreach (var (_, handler) in GetHandlers())
			handler.Manipulate(select, target, targetMx);
	}

	public void EndTransform() {
		this.History.End();
	}
	
	// Events

	private void OnSelectionChanged(SelectState state, SceneObject item) {
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

		var target = scene.FindObjectTypeById<ITransform>(action.TargetId);
		if (target is null) return false;

		switch (mod) {
			case HistoryMod.Undo when action.Initial != null:
				Manipulate(target, action.Initial.Value);
				break;
			case HistoryMod.Redo when action.Final != null:
				Manipulate(target, action.Final.Value);
				break;
			default:
				return false;
		}
		
		return true;
	}
}
