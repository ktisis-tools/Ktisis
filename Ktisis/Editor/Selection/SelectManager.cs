using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Context.Types;
using Ktisis.Events;
using Ktisis.Scene;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Services.Game;

namespace Ktisis.Editor.Selection;

public enum SelectMode {
	Default,
	Multiple,
	Force
}

public delegate void SelectChangedHandler(ISelectManager sender);

public interface ISelectManager {
	public event SelectChangedHandler Changed;
	
	public void Update();
	
	public int Count { get; }
	
	public IEnumerable<SceneEntity> GetSelected();

	public SceneEntity? GetFirstSelected();

	public bool IsActorSelected(ActorEntity actor);

	public bool IsSelected(SceneEntity entity);

	public void Select(SceneEntity entity, SelectMode mode = SelectMode.Default);
	public void Unselect(SceneEntity entity);
	
	public void Clear();
}

public class SelectManager : ISelectManager {
	private readonly IEditorContext _context;
	private readonly GPoseService _gpose;

	private readonly Event<Action<ISelectManager>> _changed = new();
	public event SelectChangedHandler Changed {
		add => this._changed.Add(value.Invoke);
		remove => this._changed.Remove(value.Invoke);
	}

	private readonly HashSet<ActorEntity> PreviousActors = new();
	private readonly List<SceneEntity> Selected = new();
	private IGameObject? Targeted;
	
	public SelectManager(
		IEditorContext context,
		GPoseService gpose
	) {
		this._context = context;
		this._gpose = gpose;
	}
	
	// Update handler

	public void Update() {
		// remove any invalid entities and flag selection changed if we do
		var remove = this.Selected.RemoveAll(item => !item.IsValid);
		if (remove > 0) this.InvokeChanged();

		// check SelectOnTarget and update selection if we should
		if (this._context.Config.Editor.SelectOnTarget && this.Targeted is not null && this._gpose.GPoseTarget is not null && !this.Targeted.Equals(this._gpose.GPoseTarget)) {
			var actor = this._context.Scene.GetEntityForIndex(this._gpose.GPoseTarget.ObjectIndex);
			if (actor is not null)
				this.Select(actor, SelectMode.Force);
		}

		// handle application of presets on any changed active actors
		if (this._context.Config.Overlay.PresetsOnActiveActor) {
			// handle preset update based on gpose target change
			if (this._context.Config.Overlay.ActiveStateType is ActiveState.Target or ActiveState.Both) {
				if (this.Targeted is not null && this._gpose.GPoseTarget is not null && !this.Targeted.Equals(this._gpose.GPoseTarget)) {
					var prevTarget = this._context.Scene.GetEntityForIndex(this.Targeted.ObjectIndex);
					var newTarget = this._context.Scene.GetEntityForIndex(this._gpose.GPoseTarget.ObjectIndex);
					if (prevTarget is not null && newTarget is not null)
						foreach (var preset in prevTarget.GetPresets().Where(p => p.isEnabled == PresetState.Enabled)) {
							newTarget.TogglePreset(preset.name, true);
							prevTarget.TogglePreset(preset.name, false);
						}
				}
			}

			// handle preset update based on selection change
			if (this._context.Config.Overlay.ActiveStateType is ActiveState.Selection or ActiveState.Both) {
				var actorsInSelection = this._context.Scene.Children.OfType<ActorEntity>().Where(this.IsActorSelected).ToList();

				// only need to do any preset stuff if we had actor/s selected previously
				if (this.PreviousActors.Count > 0) {
					// all selected actors will have equivalent presets to work with
					var presets = this.PreviousActors.First().GetPresets().ToList();

					// for actors in selection and not prev, post their presets
					foreach (var actor in actorsInSelection.Except(this.PreviousActors))
						foreach (var preset in presets.Where(p => p.isEnabled == PresetState.Enabled))
							actor.TogglePreset(preset.name, true);

					// for actors in prev and not selection, clear their presets
					foreach (var actor in this.PreviousActors.Except(actorsInSelection))
						foreach (var preset in presets.Where(p => p.isEnabled == PresetState.Enabled))
							actor.TogglePreset(preset.name, false);
				}

				// flush and update internal previous selections for next frame comparisons
				this.PreviousActors.Clear();
				this.PreviousActors.UnionWith(actorsInSelection);
			}
		}

		// update internal previous target to compare next frame
		this.Targeted = this._gpose.GPoseTarget;
	}
	
	// Selection

	public int Count => this.Selected.Count;
	
	public IEnumerable<SceneEntity> GetSelected() => this.Selected.AsReadOnly();

	public SceneEntity? GetFirstSelected() => this.Selected.FirstOrDefault();

	public bool IsSelected(SceneEntity entity)
		=> this.Selected.Contains(entity);

	public bool IsActorSelected(ActorEntity actor) {
		foreach (var target in this.GetSelected()) {
			if (
				target switch {
					BoneNode node => node.Pose.Parent,
					BoneNodeGroup group => group.Pose.Parent,
					EntityPose pose => pose.Parent,
					_ => target
				} is ActorEntity targetActor && targetActor == actor
			) return true;
		}
		return false;
	}

	public void Select(SceneEntity entity) {
		this.Selected.Remove(entity);
		this.Selected.Add(entity);
		this.InvokeChanged();
	}

	public void Select(SceneEntity entity, SelectMode mode) {
		if (mode == SelectMode.Force) {
			if (this.IsSelected(entity) && this.Count == 1)
				return;
			this.Selected.Clear();
			this.Selected.Add(entity);
			this.InvokeChanged();
			return;
		}
		
		var isSelect = this.IsSelected(entity);
		var isMulti = this.Count > 1;
		var modeMulti = mode == SelectMode.Multiple;
		if (!modeMulti)
			this.Selected.Clear();

		if (!isSelect || !modeMulti && isMulti)
			this.Selected.Add(entity);
		else
			this.Selected.Remove(entity);
		
		this.InvokeChanged();
	}

	public void Unselect(SceneEntity entity) {
		if (this.Selected.Remove(entity))
			this.InvokeChanged();
	}

	public void Clear() {
		this.Selected.Clear();
		this.InvokeChanged();
	}
	
	// Event invocation

	private void InvokeChanged() {
		try {
			this._changed.Invoke(this);
		} catch (Exception err) {
			Ktisis.Log.Error(err.ToString());
		}
	}
}
