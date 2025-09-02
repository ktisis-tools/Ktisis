using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;

using Ktisis.Data.Config;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Editor.Transforms.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Structs.Actors;

namespace Ktisis.Interface.Editor.Properties;

public class ActorPropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;
	private readonly ConfigManager _cfg;
	private readonly LocaleManager _locale;
	private static Dictionary<GazeControl, TransformTable>? GazeTables;

	private bool IsLinked {
		get => this._ctx.Config.Editor.LinkedGaze;
		set => this._ctx.Config.Editor.LinkedGaze = value;
	}
	
	public ActorPropertyList(
		IEditorContext ctx,
		ConfigManager cfg,
		LocaleManager locale
	) {
		this._ctx = ctx;
		this._cfg = cfg;
		this._locale = locale;
	}
	
	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (
			entity switch {
				BoneNode node => node.Pose.Parent,
				EntityPose pose => pose.Parent,
				_ => entity
			} is not ActorEntity actor
		) return;

		builder.AddHeader("Actor", () => this.DrawActorTab(actor), priority: 0);
		builder.AddHeader("Gaze Control", () => this.DrawGazeTab(actor), priority: 1);
	}
	
	// Actor tab

	private const string ImportOptsPopupId = "##KtisisCharaImportOptions";

	private void DrawActorTab(ActorEntity actor) {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		
		// Position lock
		
		var posLock = this._ctx.Animation.PositionLockEnabled;
		if (ImGui.Checkbox(this._locale.Translate("actors.pos_lock"), ref posLock))
			this._ctx.Animation.PositionLockEnabled = posLock;
		
		ImGui.Spacing();
		
		// Open appearance editor

		if (Buttons.IconButton(FontAwesomeIcon.Edit))
			this._ctx.Interface.OpenActorEditor(actor);
		ImGui.SameLine(0, spacing);
		ImGui.Text("Edit actor appearance");
		
		ImGui.Spacing();
		
		// Import/export

		if (ImGui.Button("Import"))
			this._ctx.Interface.OpenCharaImport(actor);
		ImGui.SameLine(0, spacing);
		if (ImGui.Button("Export"))
			this._ctx.Interface.OpenCharaExport(actor);
	}
	
	// Gaze tab
	// TODO: only draw if posing is not enabled

	private void DrawGazeTab(ActorEntity actor) {
		if (GazeTables == null)
			GazeTables = new();

		// work from existing gaze on ActorEntity or make a new one if its our first touch w them
		var gaze = actor.Gaze != null ? (ActorGaze)actor.Gaze : new ActorGaze();

		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		var result = false;

		var icon = IsLinked ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink;
		if (Buttons.IconButton(icon)) {
			if (IsLinked) {
				var move = gaze.Other;
				if (move.Gaze.Mode != 0) {
					result = true;
					gaze.Head = move;
					gaze.Eyes = move;
					gaze.Torso = move;
					gaze.Other.Gaze.Mode = GazeMode.Disabled;
				}
			}
			IsLinked = !IsLinked;
		}
		ImGui.SameLine(0, spacing);
		ImGui.Text(IsLinked ? "Linked" : "Unlinked");

		ImGui.Spacing();

		if (IsLinked)
			result |= DrawGaze(actor, ref gaze.Other.Gaze, GazeControl.All);
		else {
			result |= DrawGaze(actor, ref gaze.Eyes.Gaze, GazeControl.Eyes);
			ImGui.Spacing();
			result |= DrawGaze(actor, ref gaze.Head.Gaze, GazeControl.Head);
			ImGui.Spacing();
			result |= DrawGaze(actor, ref gaze.Torso.Gaze, GazeControl.Torso);
		}

		if (result)
			actor.Gaze = gaze;

		return;
	}

	private unsafe bool DrawGaze(ActorEntity actor, ref Gaze gaze, GazeControl type) {
		if (!GazeTables.ContainsKey(type))
			GazeTables.Add(type, new TransformTable(this._cfg));

		var result = false;
		var enabled = gaze.Mode != 0;
		var actorCharacter = (CharacterEx*)actor.Character;

		if (ImGui.Checkbox($"{type}", ref enabled)) {
			result = true;
			gaze.Mode = enabled ? GazeMode.Target : GazeMode.Disabled;
		}

		// TODO: gizmo

		// TODO: camera tracking
		// var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		// ImGui.SameLine(0, spacing);
		// var isTracking = gaze.Mode == GazeMode._KtisisFollowCam_;
		// var icon = isTracking ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
		// if (Buttons.IconButton(icon)) {
		// 	result = true;
		// 	enabled = true;
		// 	gaze.Mode = isTracking ? GazeMode.Target : GazeMode._KtisisFollowCam_;
		// }

		// positions
		if (type != GazeControl.All) {
			var baseGaze = actorCharacter->Gaze[type];
			if (baseGaze.Mode != 0 && !enabled && !result)
				gaze.Pos = baseGaze.Pos;
		}

		GazeTables[type].DrawPosition(ref gaze.Pos, TransformTableFlags.UseAvailable);

		return result;
	}
}
