using System.Collections.Generic;
using System.Numerics;
using System.Linq;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
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
using Ktisis.Interface.Overlay;
using Ktisis.Interface;

namespace Ktisis.Interface.Editor.Properties;

public class ActorPropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;
	private readonly ConfigManager _cfg;
	private readonly GuiManager _gui;
	private readonly LocaleManager _locale;
	private static Dictionary<GazeControl, TransformTable>? GazeTables;
	private static Dictionary<GazeControl, bool>? IsGizmo;

	private bool IsLinked {
		get => this._ctx.Config.Editor.LinkedGaze;
		set => this._ctx.Config.Editor.LinkedGaze = value;
	}
	
	public ActorPropertyList(
		IEditorContext ctx,
		ConfigManager cfg,
		LocaleManager locale,
		GuiManager gui
	) {
		this._ctx = ctx;
		this._cfg = cfg;
		this._locale = locale;
		this._gui = gui;
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

	private unsafe void DrawGazeTab(ActorEntity actor) {
		if (GazeTables == null)
			GazeTables = new();
		if (IsGizmo == null)
			IsGizmo = new();

		// work from existing gaze on ActorEntity or make a new one if its our first touch w them
		var gaze = actor.Gaze != null ? (ActorGaze)actor.Gaze : new ActorGaze();
		// if human actor, enable link/unlinked controls
		bool isHuman = actor.GetHuman() != null;

		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		var result = false;

		using (ImRaii.Disabled(this._ctx.Posing.IsEnabled)) {
			if (isHuman) {
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
					ClearGizmo();
				}
				ImGui.SameLine(0, spacing);
				ImGui.Text(IsLinked ? "Linked" : "Unlinked");
				ImGui.Spacing();
			}

			if (IsLinked || !isHuman)
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
		}

		return;
	}

	private unsafe bool DrawGaze(ActorEntity actor, ref Gaze gaze, GazeControl type) {
		if (!GazeTables.ContainsKey(type))
			GazeTables.Add(type, new TransformTable(this._cfg));
		if (!IsGizmo.ContainsKey(type))
			IsGizmo.Add(type, false);

		using var _ = ImRaii.PushId($"Gaze_{type}");
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		var result = false;
		var enabled = gaze.Mode != 0;
		var actorCharacter = (CharacterEx*)actor.Character;
		var isTracking = gaze.Mode == GazeMode._KtisisFollowCam_;

		if (type != GazeControl.All || !enabled) {
			// if we're changing individual gazes (or viewing All w/o activation), set each to the basegaze for that type
			// for all when not enabled, this will zero it out
			var baseGaze = actorCharacter->Gaze[type];
			gaze.Pos = baseGaze.Pos;
		} else {
			// if we're changing ALL gazes and enabled, load in any as theyre all equal to gaze.Pos for the transformtable
			var baseGaze = actorCharacter->Gaze[GazeControl.Torso];
			gaze.Pos = baseGaze.Pos;
		}

		if (ImGui.Checkbox($"{type}", ref enabled)) {
			result = true;
			gaze.Mode = enabled ? GazeMode.Target : GazeMode.Disabled;
			if (!enabled)
				ClearGizmo();
		}

		// calc button space for camera and gizmo buttons
		var btnSpace = Icons.CalcIconSize(FontAwesomeIcon.Eye).X
			+ Icons.CalcIconSize(FontAwesomeIcon.LocationArrow).X
			+ spacing * 3;
		ImGui.SameLine(0, spacing);
		ImGui.SameLine(0, ImGui.GetContentRegionAvail().X - btnSpace);

		// camera tracking - when pressed, toggle enabled and change gaze mode to KtisisFollowCam (or revert to Target mode)
		using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), isTracking)) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Eye, "Camera Tracking", Vector2.Zero)) {
				result = true;
				enabled = true;
				gaze.Mode = isTracking ? GazeMode.Target : GazeMode._KtisisFollowCam_;
				ClearGizmo();
			}
		}
		ImGui.SameLine(0, spacing);

		// TODO: this is a wack calling pattern
		// gizmo tracking - when pressed,
		// 	- toggle enabled
		// 	- take over from any followcam
		// 	- disable any other IsGizmo gaze types
		// 	- draw a translate gizmo at the targeted gaze position
		using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), IsGizmo[type])) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.LocationArrow, "Gizmo Tracking", Vector2.Zero)) {
				result = true;
				if (isTracking || !enabled) {
					gaze.Mode = GazeMode.Target;
					enabled = true;
				}

				// toggle this specific gizmotype
				if (IsGizmo[type])
					ClearGizmo();
				else {
					ClearGizmo();
					IsGizmo[type] = true;
				}
			}
		}

		if (enabled && IsGizmo[type] && !this._ctx.Posing.IsEnabled)
			result |= this._gui.Get<OverlayWindow>().DrawGazeGizmo(ref gaze.Pos);

		using (ImRaii.Disabled(!enabled))
			result |= GazeTables[type].DrawPosition(ref gaze.Pos, TransformTableFlags.UseAvailable);

		return result;
	}

	private void ClearGizmo() {
		if (IsGizmo == null) return;
		foreach (var key in IsGizmo.Keys)
			IsGizmo[key] = false;
	}
}
