using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Data.Config;
using Ktisis.Editor;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Transforms;
using Ktisis.ImGuizmo;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Types;
using Ktisis.Services;
using Ktisis.Services.Game;

namespace Ktisis.Interface.Windows;

public class TransformWindow : KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly Gizmo2D _gizmo;

	private readonly TransformTable _table;
	
	private readonly CameraService _camera;

	public TransformWindow(
		IEditorContext ctx,
		Gizmo2D gizmo,
		TransformTable table,
		CameraService camera
	) : base(
		"Transform Editor",
		ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize
	) {
		this._ctx = ctx;
		this._gizmo = gizmo;
		this._table = table;
		this._camera = camera;
	}
	
	private ITransformMemento? Transform;

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context for transform window is stale, closing...");
		this.Close();
	}

	public override void PreDraw() {
		var width = TransformTable.CalcWidth() + ImGui.GetStyle().WindowPadding.X * 2;
		this.SizeConstraints = new WindowSizeConstraints {
			MaximumSize = new Vector2(width, -1)
		};
	}

	public override void Draw() {
		this.DrawToggles();

		var target = this._ctx.Transform.Target;
		var transform = target?.GetTransform() ?? new Transform();

		var disabled = target == null;
		using var _ = ImRaii.Disabled(disabled);

		var moved = this.DrawTransform(ref transform, out var isEnded, disabled);
		if (target != null && moved) {
			this.Transform ??= this._ctx.Transform.Begin(target);
			this.Transform.SetTransform(transform);
		}

		if (isEnded) {
			this.Transform?.Dispatch();
			this.Transform = null;
		}
	}

	private bool DrawTransform(ref Transform transform, out bool isEnded, bool disabled) {
		isEnded = false;
		
		var gizmo = false;
		if (!this._ctx.Config.Editor.TransformHide) {
			gizmo = this.DrawGizmo(ref transform, TransformTable.CalcWidth(), disabled);
			isEnded = this._gizmo.IsEnded;
		}

		var table = this._table.Draw(transform, out var result);
		if (table) transform = result;
		isEnded |= this._table.IsDeactivated;

		return gizmo || table;
	}

	private void DrawToggles() {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		var iconSize = UiBuilder.IconFont.FontSize * 2;
		var iconBtnSize = new Vector2(iconSize, iconSize);

		var mode = this._ctx.Config.Gizmo.Mode;
		var modeIcon = mode == Mode.World ? FontAwesomeIcon.Globe : FontAwesomeIcon.Home;
		var modeKey = mode == Mode.World ? "world" : "local";
		var modeHint = this._ctx.Locale.Translate($"transform_edit.mode.{modeKey}");
		if (Buttons.IconButtonTooltip(modeIcon, modeHint, iconBtnSize))
			this._ctx.Config.Gizmo.Mode = mode == Mode.World ? Mode.Local : Mode.World;
		
		ImGui.SameLine(0, spacing);

		var visible = this._ctx.Config.Gizmo.Visible;
		var visIcon = visible ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
		var visHint = this._ctx.Locale.Translate("actions.Gizmo_Toggle");
		if (Buttons.IconButtonTooltip(visIcon, visHint, iconBtnSize))
			this._ctx.Config.Gizmo.Visible = !visible;

		ImGui.SameLine(0, spacing);

		var isMirror = this._ctx.Config.Gizmo.MirrorRotation;
		var flagIcon = isMirror ? FontAwesomeIcon.ArrowDownUpAcrossLine : FontAwesomeIcon.GripLines;
		var flagKey = isMirror ? "mirror" : "parallel";
		var flagHint = this._ctx.Locale.Translate($"transform_edit.flags.{flagKey}");
		if (Buttons.IconButtonTooltip(flagIcon, flagHint, iconBtnSize))
			this._ctx.Config.Gizmo.MirrorRotation ^= true;
		
		ImGui.SameLine(0, spacing);

		var avail = ImGui.GetContentRegionAvail().X;
		if (avail > iconSize)
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail - iconSize);

		var hide = this._ctx.Config.Editor.TransformHide;
		var gizmoIcon = hide ? FontAwesomeIcon.CaretUp : FontAwesomeIcon.CaretDown;
		var gizmoKey = hide ? "show" : "hide";
		var gizmoHint = this._ctx.Locale.Translate($"transform_edit.gizmo.{gizmoKey}");
		if (Buttons.IconButtonTooltip(gizmoIcon, gizmoHint, iconBtnSize))
			this._ctx.Config.Editor.TransformHide = !hide;
	}

	private unsafe bool DrawGizmo(ref Transform transform, float width, bool disabled) {
		var pos = ImGui.GetCursorScreenPos();
		var size = new Vector2(width, width);

		this._gizmo.Begin(size);
		this._gizmo.Mode = this._ctx.Config.Gizmo.Mode;
		
		this.DrawGizmoCircle(pos, size, width);
		if (disabled) {
			this._gizmo.End();
			return false;
		}
		
		var camera = this._camera.GetGameCamera();
		var cameraFov = camera != null ? camera->FoV : 1.0f;
		var cameraPos = camera != null ? (Vector3)camera->CameraBase.SceneCamera.Object.Position : Vector3.Zero;
		
		var matrix = transform.ComposeMatrix();
		this._gizmo.SetLookAt(cameraPos, matrix.Translation, cameraFov);
		var result = this._gizmo.Manipulate(ref matrix, out _);
		
		this._gizmo.End();

		if (result)
			transform.DecomposeMatrix(matrix);

		return result;
	}

	private void DrawGizmoCircle(Vector2 pos, Vector2 size, float width) {
		ImGui.GetWindowDrawList().AddCircleFilled(pos + size / 2, (width * Gizmo2D.ScaleFactor) / 2.05f, 0xCF202020);
	}
}
