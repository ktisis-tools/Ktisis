using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;

using Ktisis.Common.Utility;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Transforms.Types;
using Ktisis.ImGuizmo;
using Ktisis.Interface.Components.Objects;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Types;
using Ktisis.Services.Game;

namespace Ktisis.Interface.Windows;

public class ObjectWindow : KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly Gizmo2D _gizmo;

	private readonly TransformTable _table;
	private readonly PropertyEditor _propEditor;
	private const string WindowId = "KtisisObjectEditor";

	public ObjectWindow(
		IEditorContext ctx,
		Gizmo2D gizmo,
		TransformTable table,
		PropertyEditor propEditor
	) : base(
		$"Object Editor###{WindowId}"
	) {
		this._ctx = ctx;
		this._gizmo = gizmo;
		this._table = table;
		this._propEditor = propEditor;
	}
	
	private ITransformMemento? Transform;

	public override void OnCreate() {
		this._propEditor.Prepare(this._ctx);
	}

	public override void PreOpenCheck() {
		if (this._ctx.IsValid) return;
		Ktisis.Log.Verbose("Context for transform window is stale, closing...");
		this.Close();
	}

	public override void PreDraw() {
		var width = TransformTable.CalcWidth() + ImGui.GetStyle().WindowPadding.X * 2;
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(width, 0)
		};
	}

	public override void Draw() {
		this.DrawToggles();
		
		var target = this._ctx.Transform.Target;
		this.DrawTransform(target);
		this.DrawProperties(target);
	}
	
	// Property editor: Transform
	
	private void DrawTransform(ITransformTarget? target) {
		var transform = target?.GetTransform() ?? new Transform();

		var disabled = target == null;
		using var _ = ImRaii.Disabled(disabled);

		var moved = this.DrawTransform(ref transform, out var isEnded, disabled);
		if (target != null && moved) {
			this.Transform ??= this._ctx.Transform.Begin(target);
			this.Transform.SetTransform(transform);
		}
		
		if (!isEnded) return;
		this.Transform?.Dispatch();
		this.Transform = null;
	}
	
	// Property editor: Object specific

	private void DrawProperties(ITransformTarget? target) {
		var selected = this._ctx.Selection.GetFirstSelected() ?? target?.Primary;
		if (selected != null) {
			this.WindowName = $"Object Editor - {selected.Name}###{WindowId}";
			this._propEditor.Draw(selected);
		}
	}
	
	// Transform table

	private bool DrawTransform(ref Transform transform, out bool isEnded, bool disabled) {
		isEnded = false;
		
		var gizmo = false;
		if (!this._ctx.Config.Editor.TransformHide) {
			gizmo = this.DrawGizmo(ref transform, ImGui.GetContentRegionAvail().X, disabled);
			isEnded = this._gizmo.IsEnded;
		}

		var table = this._table.Draw(
			transform,
			out var result,
			TransformTableFlags.Default | TransformTableFlags.UseAvailable
		);
		if (table) transform = result;
		isEnded |= this._table.IsDeactivated;

		return gizmo || table;
	}
	
	// Toggle options

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
	
	// Gizmo

	private unsafe bool DrawGizmo(ref Transform transform, float width, bool disabled) {
		var size = new Vector2(width, 200);

		this._gizmo.Begin(size);
		this._gizmo.Mode = this._ctx.Config.Gizmo.Mode;
		
		if (disabled) {
			this._gizmo.End();
			return false;
		}
		
		var camera = CameraService.GetGameCamera();
		var cameraFov = camera != null ? camera->FoV : 1.0f;
		var cameraPos = camera != null ? (Vector3)camera->CameraBase.SceneCamera.Object.Position : Vector3.Zero;
		
		var matrix = transform.ComposeMatrix();
		this._gizmo.SetLookAt(cameraPos, matrix.Translation, cameraFov, (size.X - ImGui.GetStyle().WindowPadding.X * 2) / (size.Y - ImGui.GetStyle().WindowPadding.Y * 2));
		var result = this._gizmo.Manipulate(ref matrix, out _);
		
		this._gizmo.End();

		if (result)
			transform.DecomposeMatrix(matrix);

		return result;
	}
}
