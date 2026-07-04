using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Style;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;

using GLib.Popups;
using GLib.Widgets;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Objects;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Types;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Types;

namespace Ktisis.Interface.Windows;

public class CameraWindow : KtisisWindow {
	private readonly IEditorContext _ctx;

	private readonly TransformTable _fixedPos;
	private readonly TransformTable _relativePos;
	private bool IsWork = false;
	private float _toolbar = 0f;
	private readonly PopupList<BoneNode> _boneList;
	private BoneNode? _selected;
	private BoneNode? _previouslyDrawn;
	private List<BoneNode> tracked;
	
	public CameraWindow(
		IEditorContext ctx,
		TransformTable fixedPos,
		TransformTable relativePos
	) : base(
		"camera_edit.title", windowId:"###KtisisCameraEditor"
	) {
		this._ctx = ctx;
		this._fixedPos = fixedPos;
		this._relativePos = relativePos;
		this._toolbar = this._ctx.Config.Editor.UseToolbar? 0.1f : 0;
		this._boneList = new PopupList<BoneNode>("##BoneList", this.DrawBoneSelect)
			.WithSearch(BoneSearchPredicate);
	}

	private const TransformTableFlags TransformFlags = TransformTableFlags.Default | TransformTableFlags.UseAvailable;

	public override void PreOpenCheck() {
		if (this._ctx is { IsValid: true, Cameras.Current: not null }) return;
		Ktisis.Log.Verbose("State for camera window is stale, closing.");
		this.Close();
	}

	public override void PreDraw() {
		this.SizeCondition = ImGuiCond.Always;
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new(TransformTable.CalcWidth(), 300.0f),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.75f
		};
		IsWork = this._ctx.Cameras.IsWorkCameraActive;
		this.WindowName = $"{Ktisis.Locale.Translate(this._localeWindowName)}{(IsWork ? " [Work Camera]" : "")}{this._windowId}";
	}
	
	public override void Draw() {
		var camera = this._ctx.Cameras.Current;
		if (camera is not { IsValid: true }) return;

		this.DrawToggles(camera);

		using (ImRaii.Disabled(IsWork)) {
			ImGui.Spacing();
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - this._toolbar);
			ImGui.InputText("##CameraName", ref camera.Name, 64);

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			this.DrawOrbitTarget(camera);

			ImGui.Spacing();
			if(!camera.IsTracking)
				this.DrawFixedPosition(camera);
			else 
				this.DrawTracking(camera);
			this.DrawRelativeOffset(camera);
			ImGui.Spacing();
			this.DrawAnglePan(camera);
			ImGui.Spacing();

		}

		ImGui.Spacing();
		this.DrawSliders(camera);
	}
	
	// Toggles

	private void DrawToggles(EditorCamera camera) {
		var collide = !camera.Flags.HasFlag(CameraFlags.NoCollide) && !IsWork;
		using (ImRaii.Disabled(IsWork)) {
			if (ImGui.Checkbox(this._ctx.Locale.Translate("camera_edit.toggles.collide"), ref collide))
				camera.Flags ^= CameraFlags.NoCollide;
		}
		
		ImGui.SameLine();
		
		var delimit = camera.Flags.HasFlag(CameraFlags.Delimit);
		if (ImGui.Checkbox(this._ctx.Locale.Translate("camera_edit.toggles.delimit"), ref delimit))
			camera.SetDelimited(delimit);
		
		this.DrawOrthographicToggle(camera);
	}

	private unsafe void DrawOrthographicToggle(EditorCamera camera) {
		if (camera.Camera == null || camera.Camera->RenderEx == null)
			return;
		
		ImGui.SameLine();
		var enabled = camera.IsOrthographic;
		if (ImGui.Checkbox(this._ctx.Locale.Translate("camera_edit.toggles.ortho"), ref enabled))
			camera.SetOrthographic(enabled);
	}
	
	// Orbit target

	private unsafe void DrawOrbitTarget(EditorCamera camera) {
		using var _ = ImRaii.PushId("CameraOrbitTarget");
		
		var target = this._ctx.Cameras.ResolveOrbitTarget(camera);
		if (target == null) return;

		var isFixed = camera.OrbitTarget != null;
		var lockIcon = isFixed ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
		var lockHint = isFixed
			? this._ctx.Locale.Translate("camera_edit.orbit.unlock")
			: this._ctx.Locale.Translate("camera_edit.orbit.lock");
		if (Buttons.IconButtonTooltip(lockIcon, lockHint))
			camera.OrbitTarget = isFixed ? null : target.ObjectIndex;

		ImGui.SameLine();

		if (Buttons.IconButtonTooltip(FontAwesomeIcon.ArrowsToCircle, $"Turn camera tracking {(camera.IsTracking ? "off" : "on")}", iconColor: camera.IsTracking ? *ImGui.GetStyleColorVec4(ImGuiCol.TabActive) : *ImGui.GetStyleColorVec4(ImGuiCol.Text))) {
			camera.IsTracking = !camera.IsTracking;
		}
		
		ImGui.SameLine();
		
		if (!camera.IsTracking) {
			var text = $"Orbiting: {target.GetNameOrFallback(this._ctx)}";
			if (isFixed)
				ImGui.Text(text);
			else
				ImGui.TextDisabled(text);

			ImGui.SameLine(0, 0);
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - (Buttons.CalcSize()) - this._toolbar); //fuckin good enough
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.Sync, this._ctx.Locale.Translate("camera_edit.offset.to_target"))) {
				var gameObject = (GameObject*)target.Address;
				var drawObject = gameObject->DrawObject;
				if (drawObject != null)
					camera.RelativeOffset = drawObject->Object.Position - gameObject->Position;
			}
		} else {
			ImGui.Text($"Tracking mode: {camera.Tracking}");
			ImGui.SameLine(0, 0);
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - (Buttons.CalcSize()) - this._toolbar);
			var button = "";
			TrackingMode next = TrackingMode.None;
			switch (camera.Tracking) {			//replace with FontAwesome chars once in Dalamud
				case TrackingMode.Follow:
					button = "2";
					next = TrackingMode.Pan;
					break;
				case TrackingMode.FollowAndPan:
					button = "0";
					next = TrackingMode.None;
					break;
				case TrackingMode.Pan:
					button = "3";
					next = TrackingMode.FollowAndPan;
					break;
				case TrackingMode.None:
					button = "1";
					next = TrackingMode.Follow;
					break;
			}
			if (ImGui.Button(button, Vector2.Create(Buttons.CalcSize()))){
				camera.Tracking = next;
			}
			if(ImGui.IsItemHovered())
				using (ImRaii.Tooltip()) {
					ImGui.Text(next.ToString());
				}
		}
	}
	
	// Positioning

	private void DrawFixedPosition(EditorCamera camera) {
		using var _ = ImRaii.PushId("CameraFixedPosition");
		
		var posVec = camera.GetPosition();
		if (posVec == null) return;

		var pos = posVec.Value;
		var isFixed = camera.FixedPosition != null;

		if (!isFixed)
			pos -= camera.RelativeOffset;
		
		var lockIcon = isFixed ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
		var lockHint = isFixed
			? this._ctx.Locale.Translate("camera_edit.position.unlock")
			: this._ctx.Locale.Translate("camera_edit.position.lock");
		if (Buttons.IconButtonTooltip(lockIcon, lockHint))
			camera.FixedPosition = isFixed ? null : pos;

		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

		using var disable = ImRaii.Disabled(!isFixed);
		if (this._fixedPos.DrawPosition(ref pos, TransformFlags))
			camera.FixedPosition = pos;
	}

	private void DrawRelativeOffset(EditorCamera camera) {
		this.DrawIconAlign(FontAwesomeIcon.Plus, out var spacing, this._ctx.Locale.Translate("camera_edit.offset.from_base"));
		ImGui.SameLine(0, spacing);
		this._relativePos.DrawPosition(ref camera.RelativeOffset, TransformFlags);
	}
	
	// Angle & Panning

	private unsafe void DrawAnglePan(EditorCamera camera) {
		var ptr = camera.Camera;
		if (ptr == null) return;

		// Camera angle
		var angleHint = this._ctx.Locale.Translate("camera_edit.angle");
		this.DrawIconAlign(FontAwesomeIcon.ArrowsSpin, out var spacing, angleHint);
		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - this._toolbar);

		var angleDeg = ptr->Angle * MathHelpers.Rad2Deg;
		if (ImGui.DragFloat2("##CameraAngle", ref angleDeg, 0.25f))
			ptr->Angle = angleDeg * MathHelpers.Deg2Rad;

		// Camera pan
		
		var panHint = this._ctx.Locale.Translate("camera_edit.pan");
		this.DrawIconAlign(FontAwesomeIcon.ArrowsAlt, out spacing, panHint);
		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - this._toolbar);
		
		var panDeg = ptr->Pan * MathHelpers.Rad2Deg;
		if (ImGui.DragFloat2("##CameraPan", ref panDeg, 0.25f)) {
			panDeg.X %= 360.0f;
			panDeg.Y %= 360.0f;
			ptr->Pan = panDeg * MathHelpers.Deg2Rad;
		}
	}

	private unsafe void DrawTracking(EditorCamera camera) {
		this.tracked = camera.Target;
		this._previouslyDrawn = null;
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Plus, "Add bone to track\nHold shift to clear tracked bones")) {
			if(!ImGui.IsKeyDown(ImGuiKey.ModShift))
				this._boneList.Open();
			else 
				camera.Target.Clear();
		}
		ImGui.SameLine();
		
		using (ImRaii.Disabled(this._ctx.Selection.GetSelected().Count(e => e.Type is EntityType.BoneNode) == 0)) {
			if (Buttons.IconButton(FontAwesomeIcon.Repeat)) {
				camera.Target.Clear();
				foreach (var sceneEntity in this._ctx.Selection.GetSelected().Where(e => e.Type is EntityType.BoneNode)) {
					var bone = (BoneNode)sceneEntity;
					camera.Target.Add(bone);
				}
			}
		}
		if(ImGui.IsItemHovered())
			using (ImRaii.Tooltip()) {
				ImGui.Text("Track selected bones");
			}

		if (this._boneList.IsOpen) {
			List<BoneNode> list = new List<BoneNode>();

			foreach (var e in this._ctx.Scene.Children) {
				list.AddRange(e.Recurse().OfType<BoneNode>().Where(e => e.Type is EntityType.BoneNode));
			}
			this._boneList.Draw(list, out this._selected);
		}
		if (this._selected != null) {
			if(camera.Target.Contains(this._selected))
				camera.Target.Remove(this._selected);
			else
				camera.Target.Add(this._selected);
			this._selected = null;
		}
			

		ImGui.SameLine();
		ImGui.Text($"Bones tracked:");
		if (camera.Target.Count == 0) {
			ImGui.SameLine();
			ImGui.Text("None");
		}
		else if (camera.Target.Count == 1) {
			ImGui.SameLine();
			ImGui.Text($"{camera.Target[0].Name} on {camera.Target[0].Root.Name}");
		} else {
			ImGui.SameLine();
			ImGui.Text("Multiple"); //build a hover for this
			if(ImGui.IsItemHovered())
				using (ImRaii.Tooltip()) {
					var groups = camera.Target.GroupBy(t => t.Root.Name);
					foreach (var g in groups) {
						Separators.SeparatorText(g.Key, textPosition:.5f);
						foreach (var node in g) {
							ImGui.Text(node.Name);
						}
					}
				}
		}

	}
	
	private static bool BoneSearchPredicate(BoneNode bone, string query)
		=> bone.Name.Contains(query, StringComparison.InvariantCultureIgnoreCase);
		
	
	private bool DrawBoneSelect(BoneNode bone, bool isFocus) {
		if (this._previouslyDrawn?.Root.Name != bone.Root.Name) {
			Separators.SeparatorText(bone.Root?.Name);
			Separators.SeparatorText(bone.Parent?.Name, textPosition: 0.5f, height:Separators.LineHeight.Middle);
		}else if (this._previouslyDrawn?.Parent?.Name != bone.Parent?.Name) {
			Separators.SeparatorText(bone.Parent?.Name, textPosition: 0.5f, height:Separators.LineHeight.Middle);
		}
		this._previouslyDrawn = bone;
		var result = ImGui.Selectable($"{bone.Name}", this.tracked.Contains(bone));
		return result;
	}
	// Sliders

	private unsafe void DrawSliders(EditorCamera camera) {
		var ptr = camera.Camera;
		if (ptr == null) return;

		var rotateHint = this._ctx.Locale.Translate("camera_edit.sliders.rotation");
		var zoomHint = this._ctx.Locale.Translate("camera_edit.sliders.zoom");
		var distanceHint = this._ctx.Locale.Translate("camera_edit.sliders.distance");
		using (ImRaii.Disabled(IsWork))
			this.DrawSliderAngle("##CameraRotate", FontAwesomeIcon.CameraRotate, ref ptr->Rotation, -180.0f, 180.0f, 0.5f, rotateHint);
		this.DrawSliderAngle("##CameraZoom", FontAwesomeIcon.Binoculars, ref ptr->Zoom, -40.0f, 100.0f, 0.5f, zoomHint);
		this.DrawSliderFloat("##CameraDistance", FontAwesomeIcon.Moon, ref ptr->Distance, ptr->DistanceMin, ptr->DistanceMax, 0.05f, distanceHint);
		if (camera.IsOrthographic) {
			var orthoHint = this._ctx.Locale.Translate("camera_edit.sliders.ortho_zoom");
			this.DrawSliderFloat("##OrthographicZoom", FontAwesomeIcon.LocationCrosshairs, ref camera.OrthographicZoom, 0.1f, 10.0f, 0.01f, orthoHint);
		}
	}

	private void DrawSliderAngle(string label, FontAwesomeIcon icon, ref float value, float min, float max, float drag, string hint = "") {
		this.DrawSliderIcon(icon, hint);
		ImGui.SliderAngle(label, ref value, min, max, "", ImGuiSliderFlags.AlwaysClamp);
		var deg = value * MathHelpers.Rad2Deg;
		if (this.DrawSliderDrag(label, ref deg, min, max, drag, true))
			value = deg * MathHelpers.Deg2Rad;
	}

	private void DrawSliderFloat(string label, FontAwesomeIcon icon, ref float value, float min, float max, float drag, string hint = "") {
		this.DrawSliderIcon(icon, hint);
		ImGui.SliderFloat(label, ref value, min, max, "");
		this.DrawSliderDrag(label, ref value, min, max, drag, false);
	}

	private void DrawSliderIcon(FontAwesomeIcon icon, string hint = "") {
		this.DrawIconAlign(icon, out var spacing, hint);
		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (ImGui.GetCursorPosX() - ImGui.GetCursorStartPos().X));
	}
	
	private bool DrawSliderDrag(string label, ref float value, float min, float max, float drag, bool angle) {
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - this._toolbar);
		return ImGui.DragFloat($"{label}##Drag", ref value, drag, min, max, angle ? "%.0f°" : "%.3f");
	}
	
	// Alignment helpers

	private void DrawIconAlign(FontAwesomeIcon icon, out float spacing, string hint = "") {
		var padding = ImGui.GetStyle().CellPadding.X;
		var iconSpace = ((UiBuilder.DefaultFontSizePx * ImGuiHelpers.GlobalScale) - Icons.CalcIconSize(icon).X) / 2;

		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + padding + iconSpace);
		Icons.DrawIcon(icon);
		if (!string.IsNullOrEmpty(hint) && ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text(hint);
		}
		spacing = padding + iconSpace + ImGui.GetStyle().ItemInnerSpacing.X;
	}
}
