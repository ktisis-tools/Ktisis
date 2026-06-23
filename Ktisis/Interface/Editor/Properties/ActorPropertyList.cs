using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Linq;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Structs.Camera;
using Ktisis.Data.Config;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Ik.TwoJoints;
using Ktisis.Editor.Posing.Ik.Types;
using Ktisis.Interface.Editor.Properties.Types;
using Ktisis.Interface.Windows.Import;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Editor.Transforms.Types;
using Ktisis.Localization;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Structs.Actors;
using Ktisis.Interface.Overlay;
using Ktisis.Interface;
using Ktisis.Interface.Editor.Popup;
using Ktisis.Scene.Decor.Ik;
using Ktisis.Scene.Entities.Skeleton.Constraints;

namespace Ktisis.Interface.Editor.Properties;

public class ActorPropertyList : ObjectPropertyList {
	private readonly IEditorContext _ctx;
	private readonly GuiManager _gui;
	private readonly ConfigManager _cfg;
	private readonly LocaleManager _locale;
	private static Dictionary<GazeControl, TransformTable>? GazeTables;
	private const string IkCfgPopup = "##IkCfgPopup";

	private bool IsLinked {
		get => this._ctx.Config.Editor.LinkedGaze;
		set => this._ctx.Config.Editor.LinkedGaze = value;
	}

	public ActorPropertyList(
		IEditorContext ctx,
		GuiManager gui,
		ConfigManager cfg,
		LocaleManager locale
	) {
		this._ctx = ctx;
		this._gui = gui;
		this._cfg = cfg;
		this._locale = locale;
	}

	public override void Invoke(IPropertyListBuilder builder, SceneEntity entity) {
		if (
			entity switch {
				BoneNode node => node.Pose.Parent,
				BoneNodeGroup group => group.Pose.Parent,
				EntityPose pose => pose.Parent,
				_ => entity
			} is not ActorEntity actor
		) return;

		builder.AddHeader(Ktisis.Locale.Translate("object_edit.actor.headers.actor"), () => this.DrawActorTab(actor), priority: 0);
		builder.AddHeader(Ktisis.Locale.Translate("object_edit.actor.headers.adv"), () => this.DrawAdvancedTab(actor), priority: 2);
	}

	// Actor tab

	private const string ImportOptsPopupId = "##KtisisCharaImportOptions";

	private void DrawActorTab(ActorEntity actor) {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		

		ImGui.Spacing();

		// Open appearance editor

		if (Buttons.IconButton(FontAwesomeIcon.Edit))
			this._ctx.Interface.OpenActorEditor(actor);
		ImGui.SameLine(0, spacing);
		ImGui.Text(Ktisis.Locale.Translate("object_edit.actor.chara_edit"));

		ImGui.Spacing();

		// Import/export

		if (ImGui.Button(Ktisis.Locale.Translate("object_edit.actor.export")))
			this._ctx.Interface.OpenCharaExport(actor);

		ImGui.Spacing();
		Separators.SeparatorText(Ktisis.Locale.Translate("object_edit.actor.headers.import"), textColor:ImGui.GetColorU32(ImGuiCol.Header));
		ImGui.Spacing();

		var embedEditor = this._gui.GetOrCreate<CharaImportDialog>(this._ctx);
		embedEditor.OnOpen();
		embedEditor.SetTarget(actor);
		embedEditor.DrawEmbed();
	}

	// Advanced tab

	private void DrawAdvancedTab(ActorEntity actor) {
		Separators.SeparatorText(Ktisis.Locale.Translate("object_edit.actor.headers.gaze"), textColor:ImGui.GetColorU32(ImGuiCol.Header));
		this.DrawGazeTab(actor);

		if (!TryGetEntityPose(actor, out var pose) || pose.IkController.GroupCount == 0)
			return;

		ImGui.Spacing();
		Separators.SeparatorText(Ktisis.Locale.Translate("object_edit.actor.headers.ik"), textColor:ImGui.GetColorU32(ImGuiCol.Header));
		ImGui.Spacing();

		
		this.DrawConstraintsTab(pose);
	}

	private unsafe void DrawGazeTab(ActorEntity actor) {
		if (GazeTables == null)
			GazeTables = new();

		// work from existing gaze on ActorEntity or make a new one if its our first touch w them
		var gaze = actor.Gaze != null ? (ActorGaze)actor.Gaze : new ActorGaze();
		// if human actor, enable link/unlinked controls
		bool isHuman = actor.GetHuman() != null;

		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		var result = false;

		using (ImRaii.Disabled(this._ctx.Posing.IsEnabled)) {
			DrawActorTargeting(actor);
			if (isHuman) {
				var icon = IsLinked ? FontAwesomeIcon.Link : FontAwesomeIcon.Unlink;
				if (Buttons.IconButton(icon)) {
					if (IsLinked) {
						var move = gaze.Other;
						if (move.Gaze.Mode != 0) {
							move.Gaze.Mode = GazeMode.Target;

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
				ImGui.Text(IsLinked ? Ktisis.Locale.Translate("object_edit.actor.gaze.linked") : Ktisis.Locale.Translate("object_edit.actor.gaze.unlinked"));
				ImGui.Spacing();
			}

			var anyGizmo = gaze.Other.Gaze.Mode == GazeMode._KtisisFollowGizmo_
				|| gaze.Eyes.Gaze.Mode == GazeMode._KtisisFollowGizmo_
				|| gaze.Head.Gaze.Mode == GazeMode._KtisisFollowGizmo_
				|| gaze.Torso.Gaze.Mode == GazeMode._KtisisFollowGizmo_;

			if (IsLinked || !isHuman)
				result |= DrawGaze(actor, ref gaze.Other.Gaze, GazeControl.All, anyGizmo);
			else {
				result |= DrawGaze(actor, ref gaze.Eyes.Gaze, GazeControl.Eyes, anyGizmo);
				ImGui.Spacing();
				result |= DrawGaze(actor, ref gaze.Head.Gaze, GazeControl.Head, anyGizmo);
				ImGui.Spacing();
				result |= DrawGaze(actor, ref gaze.Torso.Gaze, GazeControl.Torso, anyGizmo);
			}

			if (!anyGizmo || this._ctx.Posing.IsEnabled) {
				var overlay = this._gui.Get<OverlayWindow>();
				overlay.GazeTarget = null;
			}

			if (result)
				actor.Gaze = gaze;
		}
		return;
	}

	private unsafe bool DrawGaze(ActorEntity actor, ref Gaze gaze, GazeControl type, bool anyGizmo) {
		if (!GazeTables.ContainsKey(type))
			GazeTables.Add(type, new TransformTable(this._cfg, this._locale));

		using var _ = ImRaii.PushId($"Gaze_{type}");
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		var result = false;
		var enabled = gaze.Mode != 0;
		var actorCharacter = (CharacterEx*)actor.Character;
		var isTracking = gaze.Mode == GazeMode._KtisisFollowCam_;
		var isGizmo = gaze.Mode == GazeMode._KtisisFollowGizmo_;

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
			// if enabling via checkbox, set the position to a lerp instead of world origin
			if (enabled)
				gaze.Pos = GetCameraLerpFor(actor);
			gaze.Mode = enabled ? GazeMode.Target : GazeMode.Disabled;
		}

		// calc button space for camera and gizmo buttons
		var btnSpace = Icons.CalcIconSize(FontAwesomeIcon.Eye).X
			+ Icons.CalcIconSize(FontAwesomeIcon.LocationArrow).X
			+ spacing * 5;
		ImGui.SameLine(0, spacing);

		ImGui.SameLine(0, 0);
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnSpace - ImGui.GetStyle().WindowPadding.X);  

		// camera tracking - when pressed, toggle enabled and change gaze mode to KtisisFollowCam (or revert to Target mode)
		using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), isTracking)) {
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Eye, Ktisis.Locale.Translate("object_edit.actor.gaze.camera"), Vector2.Zero)) {
				result = true;
				enabled = true;
				gaze.Mode = isTracking ? GazeMode.Target : GazeMode._KtisisFollowCam_;
			}
		}
		ImGui.SameLine(0, spacing);

		// gizmo tracking - when pressed,
		// 	- toggle enabled
		// 	- take over from any followcam
		// 	- prevent any other gizmo gazing
		// 	- draw a translate gizmo at the targeted gaze position
		using (ImRaii.Disabled(anyGizmo && !isGizmo)) {
			using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), isGizmo)) {
				if (Buttons.IconButtonTooltip(FontAwesomeIcon.LocationArrow, Ktisis.Locale.Translate("object_edit.actor.gaze.gizmo"), Vector2.Zero)) {
					// if this wasnt enabled, set the gaze target to a friendly lerp
					if (!enabled)
						gaze.Pos = GetCameraLerpFor(actor);

					result = true;
					enabled = true;
					gaze.Mode = isGizmo ? GazeMode.Target : GazeMode._KtisisFollowGizmo_;
				}
			}
		}

		var overlay = this._gui.Get<OverlayWindow>();
		if (enabled && isGizmo && !this._ctx.Posing.IsEnabled) {
			if (overlay.GazeTarget == null) {
				overlay.GazeTarget = gaze.Pos;
			} else if (overlay.GazeManipulated) {
				gaze.Pos = (Vector3)overlay.GazeTarget;
				result = true;
			}
		}

		using (ImRaii.Disabled(!enabled || isTracking || isGizmo))
			result |= GazeTables[type].DrawPosition(ref gaze.Pos, TransformTableFlags.UseAvailable);

		return result;
	}

	private unsafe void DrawActorTargeting(ActorEntity actor) {
		// skip button+label if:
		// actor is not PC (since we cant force a new target on non-pcs yet)
		// AND actor is not PC with no known target
		var targetId = actor.GetActorGazeTarget();
		if (!actor.Actor.IsPcCharacter() && targetId == 0) return;

		// 1. select button to choose entities in scene to set to target
		// 2. show current target if one is set
		// todo: can you unset targets?
		// button
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Users, Ktisis.Locale.Translate("object_edit.actor.gaze.target")))
			this._gui.CreatePopup<ActorGazeTargetPopup>(this._ctx, actor).Open();

		// label
		ActorEntity? targetEntity = null;
		var currentActors = this._ctx.Scene.Children
			.OfType<ActorEntity>()
			.ToList();
		foreach (ActorEntity ent in currentActors)
			if (ent.Actor.ObjectIndex == targetId)
				targetEntity = ent;

		ImGui.AlignTextToFramePadding();
		var hasTarget = targetId != 0;
		var label = hasTarget ? $"{Ktisis.Locale.Translate("object_edit.actor.gaze.targeting")}: {(targetEntity != null ? targetEntity.Name : $"{Ktisis.Locale.Translate("object_edit.actor.gaze.unk")} ({targetId})")}" : Ktisis.Locale.Translate("object_edit.actor.gaze.null");
		using (ImRaii.Disabled(!hasTarget)) {
			ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
			ImGui.Text(label);
		}

		return;
	}

	private unsafe Vector3 GetCameraLerpFor(ActorEntity actor) {
		var camera = GameCameraEx.GetActive();
		return camera != null ? Vector3.Lerp(actor.CsGameObject->Position, camera->Position, 0.5f) : actor.CsGameObject->Position;
	}

	// IK Section

	private void DrawConstraintsTab(EntityPose pose) {
		var style = ImGui.GetStyle();
		var spacing = style.ItemInnerSpacing.X;

		var anyEnabled = pose.IkController.GetGroups().Any(p => p.group.IsEnabled);
		var allEnabled = pose.IkController.GetGroups().All(p => p.group.IsEnabled);

		using (ImRaii.Disabled(allEnabled)) {
			if (ImGui.Button(Ktisis.Locale.Translate("transform_edit.ik.enable_all"))) {
				foreach (var (name, group) in pose.IkController.GetGroups().Where((tuple => !tuple.group.IsEnabled))) {
					if (!TryGetGroupEndNode(pose, group, out var node))
						continue;
					node.Toggle();
				}
			}
		}
		ImGui.SameLine();
		using (ImRaii.Disabled(!anyEnabled)) {
			if (ImGui.Button(Ktisis.Locale.Translate("transform_edit.ik.disable_all"))) {
				foreach (var (name, group) in pose.IkController.GetGroups().Where((tuple => tuple.group.IsEnabled))) {
					if (!TryGetGroupEndNode(pose, group, out var node))
						continue;
					node.Toggle();
				}
			}
		}
		
		foreach (var (name, group) in pose.IkController.GetGroups()) {
			if (!TryGetGroupEndNode(pose, group, out var node))
				continue;

			using var _ = ImRaii.PushId($"IkProp_{name}");

			var enabled = group.IsEnabled;
			if (ImGui.Checkbox(" " + this._locale.Translate($"boneCategory.{name}"), ref enabled))
				node.Toggle();

			var btnSpace = Icons.CalcIconSize(FontAwesomeIcon.HandPointer).X
				+ Icons.CalcIconSize(FontAwesomeIcon.EllipsisH).X
				+ spacing * 5;


			ImGui.SameLine(0, 0);
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - btnSpace - ImGui.GetStyle().WindowPadding.X);  

			using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive), node.IsSelected)) {
				var canSelect = !node.IsSelected || this._ctx.Selection.Count > 1;
				if (Buttons.IconButtonTooltip(FontAwesomeIcon.HandPointer, Ktisis.Locale.Translate("transform_edit.ik.target"), Vector2.Zero) && canSelect)
					node.Select(GuiHelpers.GetSelectMode());
			}

			ImGui.SameLine(0, spacing);

			if (Buttons.IconButtonTooltip(FontAwesomeIcon.EllipsisH, Ktisis.Locale.Translate("transform_edit.ik.edit"), Vector2.Zero))
				ImGui.OpenPopup(IkCfgPopup);

			if (!ImGui.IsPopupOpen(IkCfgPopup)) continue;

			using var popup = ImRaii.Popup(IkCfgPopup);
			if (popup.Success) this.DrawIkConfig(node);
		}
	}

	private void DrawIkConfig(IIkNode ik) {
		var isEnabled = ik.IsEnabled;
		if (ImGui.Checkbox(Ktisis.Locale.Translate("transform_edit.ik.active"), ref isEnabled)) {
			if (isEnabled)
				ik.Enable();
			else
				ik.Disable();
		}

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		switch (ik) {
			case ICcdNode node:
				this.DrawCcd(node);
				break;
			case ITwoJointsNode node:
				this.DrawTwoJoints(node);
				break;
		}
	}

	// IK: CCD

	private void DrawCcd(ICcdNode node) {
		ImGui.SliderFloat(this._locale.Translate("transform_edit.ik.ccd.gain"), ref node.Group.Gain, 0.0f, 1.0f, "%.2f");
		ImGui.SliderInt(this._locale.Translate("transform_edit.ik.ccd.iterations"), ref node.Group.Iterations, 0, 60);
	}

	// IK: Two Joints

	private void DrawTwoJoints(ITwoJointsNode node) {
		ImGui.Checkbox(this._locale.Translate("transform_edit.ik.two_joints.enforce"), ref node.Group.EnforceRotation);

		ImGui.Spacing();

		ImGui.Text(this._locale.Translate("transform_edit.ik.two_joints.mode"));
		DrawIkMode(this._locale.Translate("transform_edit.ik.two_joints.fixed"), TwoJointsMode.Fixed, node.Group);
		DrawIkMode(this._locale.Translate("transform_edit.ik.two_joints.relative"), TwoJointsMode.Relative, node.Group);

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		ImGui.Text(this._locale.Translate("transform_edit.ik.two_joints.gain"));
		ImGui.SliderFloat($"{this._locale.Translate("transform_edit.ik.two_joints.gain.shoulder")}##FirstWeight", ref node.Group.FirstBoneGain, 0.0f, 1.0f, "%.2f");
		ImGui.SliderFloat($"{this._locale.Translate("transform_edit.ik.two_joints.gain.elbow")}##SecondWeight", ref node.Group.SecondBoneGain, 0.0f, 1.0f, "%.2f");
		ImGui.SliderFloat($"{this._locale.Translate("transform_edit.ik.two_joints.gain.hand")}##HandWeight", ref node.Group.EndBoneGain, 0.0f, 1.0f, "%.2f");

		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		ImGui.Text(this._locale.Translate("transform_edit.ik.two_joints.hinges"));
		ImGui.Spacing();
		ImGui.SliderFloat(this._locale.Translate("transform_edit.ik.two_joints.hinges.min"), ref node.Group.MinHingeAngle, -1.0f, 1.0f, "%.2f");
		ImGui.SliderFloat(this._locale.Translate("transform_edit.ik.two_joints.hinges.max"), ref node.Group.MaxHingeAngle, -1.0f, 1.0f, "%.2f");
		ImGui.SliderFloat3(this._locale.Translate("transform_edit.ik.two_joints.hinges.axis"), ref node.Group.HingeAxis, -1.0f, 1.0f, "%.2f");

		ImGui.Spacing();
	}

	private static void DrawIkMode(string label, TwoJointsMode mode, TwoJointsGroup group) {
		var value = group.Mode == mode;
		if (ImGui.RadioButton(label, value))
			group.Mode = mode;
	}

	// Entity helpers

	private static bool TryGetEntityPose(SceneEntity entity, [NotNullWhen(true)] out EntityPose? result) {
		result = entity switch {
			ActorEntity actor => actor.Pose,
			BoneNodeGroup group => group.Pose,
			BoneNode node => node.Pose,
			EntityPose pose => pose,
			_ => null
		};
		return result != null;
	}

	private static bool TryGetGroupEndNode(EntityPose pose, IIkGroup group, [NotNullWhen(true)] out IkEndNode? node) {
		node = pose.Recurse().FirstOrDefault(node => node is IkEndNode {
				Parent: IkNodeGroupBase grpNode
			} && grpNode.Group == group
		) as IkEndNode;

		return node != null;
	}
}
