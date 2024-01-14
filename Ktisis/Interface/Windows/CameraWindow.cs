using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Types;

namespace Ktisis.Interface.Windows;

public class CameraWindow : KtisisWindow {
	private readonly IEditorContext _context;

	private readonly TransformTable _fixedPos;
	private readonly TransformTable _relativePos;
	
	public CameraWindow(
		IEditorContext context,
		TransformTable fixedPos,
		TransformTable relativePos
	) : base("Camera Editor") {
		this._context = context;
		this._fixedPos = fixedPos;
		this._relativePos = relativePos;
	}

	private const TransformTableFlags TransformFlags = TransformTableFlags.Default | TransformTableFlags.UseAvailable & ~TransformTableFlags.Operation;

	public override void PreOpenCheck() {
		if (this._context is { IsValid: true, Cameras.Current: not null }) return;
		Ktisis.Log.Verbose("State for camera window is stale, closing.");
		this.Close();
	}

	public override void PreDraw() {
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(TransformTable.CalcWidth(), 200.0f),
			MaximumSize = ImGui.GetIO().DisplaySize * 0.75f
		};
	}
	
	public override void Draw() {
		var camera = this._context.Cameras.Current;
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
		if (ImGui.Checkbox("Collision", ref collide))
			camera.Flags ^= CameraFlags.NoCollide;
		
		ImGui.SameLine();
		
		var delimit = camera.Flags.HasFlag(CameraFlags.Delimit);
		if (ImGui.Checkbox("Delimited", ref delimit))
			camera.SetDelimited(delimit);
	}
	
	// Orbit target

	private unsafe void DrawOrbitTarget(EditorCamera camera) {
		using var _id = ImRaii.PushId("CameraOrbitTarget");
		
		var target = this._context.Cameras.ResolveOrbitTarget(camera);
		if (target == null) return;

		var isFixed = camera.OrbitTarget != null;
		var lockIcon = isFixed ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
		if (Buttons.IconButtonTooltip(lockIcon, isFixed ? "Orbit target is locked" : "Orbit target is unlocked"))
			camera.OrbitTarget = isFixed ? null : target.ObjectIndex;

		ImGui.SameLine();

		var text = $"Orbiting: {target.Name.TextValue}";
		if (isFixed)
			ImGui.Text(text);
		else
			ImGui.TextDisabled(text);
		
		ImGui.SameLine();
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - Buttons.CalcSize());
		if (Buttons.IconButtonTooltip(FontAwesomeIcon.Sync, "Offset camera to target model")) {
			var gameObject = (GameObject*)target.Address;
			var drawObject = gameObject->DrawObject;
			if (drawObject != null)
				camera.RelativeOffset = drawObject->Object.Position -  gameObject->Position;
		}
	}
	
	// Positioning

	private void DrawFixedPosition(EditorCamera camera) {
		using var _id = ImRaii.PushId("CameraFixedPosition");
		
		var posVec = camera.GetPosition();
		if (posVec == null) return;

		var pos = posVec.Value;
		var isFixed = camera.FixedPosition != null;

		if (!isFixed)
			pos -= camera.RelativeOffset;
		
		var lockIcon = isFixed ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock;
		var lockHint = isFixed ? "Camera position is locked" : "Camera position is unlocked";
		if (Buttons.IconButtonTooltip(lockIcon, lockHint))
			camera.FixedPosition = isFixed ? null : pos;

		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

		using var _disable = ImRaii.Disabled(!isFixed);
		if (this._fixedPos.DrawPosition(ref pos, TransformFlags))
			camera.FixedPosition = pos;
	}

	private void DrawRelativeOffset(EditorCamera camera) {
		this.DrawIconAlign(FontAwesomeIcon.Plus, out var spacing);
		ImGui.SameLine(0, spacing);
		this._relativePos.DrawPosition(ref camera.RelativeOffset, TransformFlags);
	}
	
	// Angle & Panning

	private unsafe void DrawAnglePan(EditorCamera camera) {
		var ptr = camera.Camera;
		if (ptr == null) return;
		
		// Camera angle
		
		this.DrawIconAlign(FontAwesomeIcon.ArrowsSpin, out var spacing);
		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		
		var angleDeg = ptr->Angle * MathHelpers.Rad2Deg;
		if (ImGui.DragFloat2("##CameraAngle", ref angleDeg, 0.25f))
			ptr->Angle = angleDeg * MathHelpers.Deg2Rad;

		// Camera pan
		
		this.DrawIconAlign(FontAwesomeIcon.ArrowsAlt, out spacing);
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

		this.DrawIconAlign(FontAwesomeIcon.CameraRotate, out var spacing);
		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		ImGui.SliderAngle("##CameraRotate", ref ptr->Rotation, -180.0f, 180.0f, "%.3f", ImGuiSliderFlags.AlwaysClamp);

		this.DrawIconAlign(FontAwesomeIcon.VectorSquare, out spacing);
		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		ImGui.SliderAngle("##CameraFoV", ref ptr->FoV, -40.0f, 100.0f, "%.3f", ImGuiSliderFlags.AlwaysClamp);

		this.DrawIconAlign(FontAwesomeIcon.Moon, out  spacing);
		ImGui.SameLine(0, spacing);
		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		ImGui.SliderFloat("##CameraDistance", ref ptr->Distance, 0.0f, ptr->DistanceMax);
	}
	
	// Alignment helpers

	private void DrawIconAlign(FontAwesomeIcon icon, out float spacing) {
		var padding = ImGui.GetStyle().CellPadding.X;
		var iconSpace = (UiBuilder.IconFont.FontSize - Icons.CalcIconSize(icon).X) / 2;
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + padding + iconSpace);
		Icons.DrawIcon(icon);
		spacing = padding + iconSpace + ImGui.GetStyle().ItemInnerSpacing.X;
	}
}
