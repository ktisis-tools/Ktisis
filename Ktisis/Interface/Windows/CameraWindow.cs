using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

using GLib.Widgets;

using Ktisis.Common.Utility;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Objects;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows;

public class CameraWindow : KtisisWindow {
	private readonly IEditorContext _ctx;

	private readonly TransformTable _fixedPos;
	private readonly TransformTable _relativePos;
	
	public CameraWindow(
		IEditorContext ctx,
		TransformTable fixedPos,
		TransformTable relativePos
	) : base(
		"Camera Editor"
	) {
		this._ctx = ctx;
		this._fixedPos = fixedPos;
		this._relativePos = relativePos;
	}

	private const TransformTableFlags TransformFlags = TransformTableFlags.Default | TransformTableFlags.UseAvailable & ~TransformTableFlags.Operation;

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
	}
	
	public override void Draw() {
		var camera = this._ctx.Cameras.Current;
		if (camera is not { IsValid: true }) return;

		this.DrawToggles(camera);
		
		ImGui.Spacing();
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		ImGui.InputText("##CameraName", ref camera.Name, 64);
		
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		
		this.DrawOrbitTarget(camera);
		ImGui.Spacing();
		this.DrawFixedPosition(camera);
		this.DrawRelativeOffset(camera);
		ImGui.Spacing();
		this.DrawAnglePan(camera);
		ImGui.Spacing();
		this.DrawSliders(camera);
	}
	
	// Toggles

	private void DrawToggles(EditorCamera camera) {
		var collide = !camera.Flags.HasFlag(CameraFlags.NoCollide);
		if (ImGui.Checkbox(this._ctx.Locale.Translate("camera_edit.toggles.collide"), ref collide))
			camera.Flags ^= CameraFlags.NoCollide;
		
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

		var text = $"Orbiting: {target.Name.TextValue}";
		if (isFixed)
			ImGui.Text(text);
		else
			ImGui.TextDisabled(text);
		
		ImGui.SameLine();
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Buttons.CalcSize());
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Sync, this._ctx.Locale.Translate("camera_edit.offset.to_target"))) {
			var gameObject = (GameObject*)target.Address;
			var drawObject = gameObject->DrawObject;
			if (drawObject != null)
				camera.RelativeOffset = drawObject->Object.Position -  gameObject->Position;
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
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

		var angleDeg = ptr->Angle * MathHelpers.Rad2Deg;
		if (ImGui.DragFloat2("##CameraAngle", ref angleDeg, 0.25f))
			ptr->Angle = angleDeg * MathHelpers.Deg2Rad;

		// Camera pan
		
		var panHint = this._ctx.Locale.Translate("camera_edit.pan");
		this.DrawIconAlign(FontAwesomeIcon.ArrowsAlt, out spacing, panHint);
		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		
		var panDeg = ptr->Pan * MathHelpers.Rad2Deg;
		if (ImGui.DragFloat2("##CameraPan", ref panDeg, 0.25f)) {
			panDeg.X %= 360.0f;
			panDeg.Y %= 360.0f;
			ptr->Pan = panDeg * MathHelpers.Deg2Rad;
		}
	}
	
	// Sliders

	private unsafe void DrawSliders(EditorCamera camera) {
		var ptr = camera.Camera;
		if (ptr == null) return;

		var rotateHint = this._ctx.Locale.Translate("camera_edit.sliders.rotation");
		var zoomHint = this._ctx.Locale.Translate("camera_edit.sliders.zoom");
		var distanceHint = this._ctx.Locale.Translate("camera_edit.sliders.distance");
		this.DrawSliderAngle("##CameraRotate", FontAwesomeIcon.CameraRotate, ref ptr->Rotation, -180.0f, 180.0f, 0.5f, rotateHint);
		this.DrawSliderAngle("##CameraZoom", FontAwesomeIcon.VectorSquare, ref ptr->Zoom, -40.0f, 100.0f, 0.5f, zoomHint);
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
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		return ImGui.DragFloat($"{label}##Drag", ref value, drag, min, max, angle ? "%.0f°" : "%.3f");
	}
	
	// Alignment helpers

	private void DrawIconAlign(FontAwesomeIcon icon, out float spacing, string hint = "") {
		var padding = ImGui.GetStyle().CellPadding.X;
		var iconSpace = (UiBuilder.IconFont.FontSize - Icons.CalcIconSize(icon).X) / 2;

		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + padding + iconSpace);
		Icons.DrawIcon(icon);
		if (!string.IsNullOrEmpty(hint) && ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text(hint);
		}
		spacing = padding + iconSpace + ImGui.GetStyle().ItemInnerSpacing.X;
	}
}
