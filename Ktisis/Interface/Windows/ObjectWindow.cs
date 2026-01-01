using System.Numerics;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using Dalamud.Interface.Utility;

using GLib.Widgets;

using Ktisis.Common.Utility;
using Ktisis.Editor.Camera.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Transforms;
using Ktisis.Editor.Transforms.Types;
using Ktisis.Editor.Selection;
using Ktisis.Interface.Components.Objects;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Types;
using Ktisis.Services.Game;
using Ktisis.Scene.Entities;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Interface.Windows;

public class ObjectWindow : KtisisWindow {
	private readonly IEditorContext _ctx;
	private readonly Gizmo2D _gizmo;
	private readonly GuiManager _gui;

	private readonly TransformTable _table;
	private readonly PropertyEditor _propEditor;
	private const string WindowId = "KtisisObjectEditor";

	public ObjectWindow(
		IEditorContext ctx,
		Gizmo2D gizmo,
		GuiManager gui,
		TransformTable table,
		PropertyEditor propEditor
	) : base(
		$"Object Editor###{WindowId}"
	) {
		this._ctx = ctx;
		this._gizmo = gizmo;
		this._gui = gui;
		this._table = table;
		this._propEditor = propEditor;
	}
	
	private ITransformMemento? Transform;

	public override void OnCreate() {
		this._propEditor.Prepare(this._ctx, this._gui);
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
		var target = this._ctx.Transform.Target;
		this.DrawToggles(target);

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

	private void DrawToggles(ITransformTarget? target) {
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;

		var iconSize = UiBuilder.DefaultFontSizePx * ImGuiHelpers.GlobalScale * 2;
		var iconBtnSize = new Vector2(iconSize, iconSize);

		var mode = this._ctx.Config.Gizmo.Mode;
		var modeIcon = mode == ImGuizmoMode.World ? FontAwesomeIcon.Globe : FontAwesomeIcon.Home;
		var modeKey = mode == ImGuizmoMode.World ? "world" : "local";
		var modeHint = this._ctx.Locale.Translate($"transform_edit.mode.{modeKey}");
		if (Buttons.IconButtonTooltip(modeIcon, modeHint, iconBtnSize))
			this._ctx.Config.Gizmo.Mode = mode == ImGuizmoMode.World ? ImGuizmoMode.Local : ImGuizmoMode.World;
		
		ImGui.SameLine(0, spacing);

		var visible = this._ctx.Config.Gizmo.Visible;
		var visIcon = visible ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
		var visHint = this._ctx.Locale.Translate("actions.Gizmo_Toggle");
		if (Buttons.IconButtonTooltip(visIcon, visHint, iconBtnSize))
			this._ctx.Config.Gizmo.Visible = !visible;

		ImGui.SameLine(0, spacing);

		var mirrorState = this._ctx.Config.Gizmo.MirrorRotation;
		var flagIcon = FontAwesomeIcon.GripLines;
		var flagKey = "parallel";
		if (mirrorState == MirrorMode.Inverse) {
			flagIcon = FontAwesomeIcon.ArrowDownUpAcrossLine;
			flagKey = "inverse";
		}
		else if (mirrorState == MirrorMode.Reflect) {
			flagIcon = FontAwesomeIcon.ArrowsLeftRightToLine;
			flagKey = "reflect";
		}
		var flagHint = this._ctx.Locale.Translate($"transform_edit.flags.{flagKey}");
		if (Buttons.IconButtonTooltip(flagIcon, flagHint, iconBtnSize))
			this._ctx.Config.Gizmo.SetNextMirrorRotation();

		ImGui.SameLine(0, spacing);

		// Sibling Link selector
		// if we have a selection & target's primary entity is a bone node, draw the button
		// if we have >1 bonenodes selected or no sibling, disable the button
		// if we have 1 bonenode selected that has a sibling, enable the button
		var selected = target?.Primary;
		var selectionCount = target?.Targets.Count();
		if (selectionCount != 0 && selected != null && selected is BoneNode bNode) {
			var siblingNode = bNode.Pose.TryResolveSibling(bNode);
			var siblingAvailable = siblingNode != null;
			var siblingKey = siblingAvailable ? (selectionCount == 1 ? "available" : "multiple") : "unavailable";
			var siblingHint = this._ctx.Locale.Translate(
				$"transform_edit.sibling.{siblingKey}",
				new Dictionary<string, string> {
						{ "bone", siblingAvailable ? siblingNode.Name : bNode.Name }
				}
			);

			using var _ = ImRaii.Disabled(!siblingAvailable || selectionCount != 1); // disable if current bone has no sibling or if multiple selections
			if (Buttons.IconButtonTooltip(FontAwesomeIcon.PeopleArrows, siblingHint, iconBtnSize))
				this._ctx.Selection.Select(siblingNode, SelectMode.Multiple); // if a sibling exists, select it assuming SelectMode.Multiple

			ImGui.SameLine(0, spacing);
		}

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

		var cameraFov = 1.0f;
		var cameraPos = Vector3.Zero;
		if (this._ctx.Cameras.IsWorkCameraActive) {
			var freeCam = (WorkCamera)this._ctx.Cameras.Current;
			cameraFov = freeCam.Camera->RenderEx->FoV;
			cameraPos = freeCam.Position;
		} else {
			var camera = CameraService.GetGameCamera();
			if (camera != null) {
				cameraFov = camera->FoV;
				cameraPos = camera->CameraBase.SceneCamera.Object.Position;
			}
		}
		
		var matrix = transform.ComposeMatrix();
		this._gizmo.SetLookAt(cameraPos, matrix.Translation, cameraFov, (size.X - ImGui.GetStyle().WindowPadding.X * 2) / (size.Y - ImGui.GetStyle().WindowPadding.Y * 2));
		var result = this._gizmo.Manipulate(ref matrix, out _);
		
		this._gizmo.End();

		if (result)
			transform.DecomposeMatrixPrecise(matrix, transform);

		return result;
	}
}
