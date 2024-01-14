using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Data.Config;
using Ktisis.Editor;
using Ktisis.Editor.Context;
using Ktisis.Editor.Transforms;
using Ktisis.ImGuizmo;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Types;
using Ktisis.Services;

namespace Ktisis.Interface.Windows;

public class TransformWindow : KtisisWindow {
	private readonly IEditorContext _context;
	private readonly Gizmo2D _gizmo;

	private readonly TransformTable _table;
	
	private readonly CameraService _camera;

	private Configuration Config => this._context.Config;
	
	private ITransformHandler Handler => this._context.Transform;

	public TransformWindow(
		IEditorContext context,
		Gizmo2D gizmo,
		TransformTable table,
		CameraService camera
	) : base(
		"Transform Editor",
		ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize
	) {
		this._context = context;
		this._gizmo = gizmo;
		this._table = table;
		this._camera = camera;
	}
	
	private ITransformMemento? Transform;

	public override void PreOpenCheck() {
		if (this._context.IsValid) return;
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

		var target = this.Handler.Target;
		var transform = target?.GetTransform() ?? new Transform();

		var disabled = target == null;
		using var _ = ImRaii.Disabled(disabled);

		var moved = this.DrawTransform(ref transform, out var isEnded, disabled);
		if (target != null && moved) {
			this.Transform ??= this.Handler.Begin(target);
			target.SetTransform(transform);
		}

		if (isEnded) {
			this.Transform?.Dispatch();
			this.Transform = null;
		}
	}

	private bool DrawTransform(ref Transform transform, out bool isEnded, bool disabled) {
		isEnded = false;
		
		var gizmo = false;
		if (!this.Config.Editor.TransformHide) {
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

		var mode = this.Config.Gizmo.Mode;
		var modeIcon = mode == Mode.World ? FontAwesomeIcon.Globe : FontAwesomeIcon.Home;
		var modeKey = mode == Mode.World ? "world" : "local";
		if (Buttons.IconButtonTooltip(modeIcon, modeKey, iconBtnSize))
			this.Config.Gizmo.Mode = mode == Mode.World ? Mode.Local : Mode.World;

		ImGui.SameLine(0, spacing);
		
		// TODO Flags

		var avail = ImGui.GetContentRegionAvail().X;
		if (avail > iconSize)
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + avail - iconSize);

		var show = this.Config.Editor.TransformHide;
		var gizmoIcon = show ? FontAwesomeIcon.CaretUp : FontAwesomeIcon.CaretDown;
		var gizmoKey = show ? "hide" : "show";
		// TODO hint
		if (Buttons.IconButtonTooltip(gizmoIcon, gizmoKey, iconBtnSize))
			this.Config.Editor.TransformHide = !show;
	}

	private unsafe bool DrawGizmo(ref Transform transform, float width, bool disabled) {
		var pos = ImGui.GetCursorScreenPos();
		var size = new Vector2(width, width);

		this._gizmo.Begin(size);
		this._gizmo.Mode = this.Config.Gizmo.Mode;
		
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
