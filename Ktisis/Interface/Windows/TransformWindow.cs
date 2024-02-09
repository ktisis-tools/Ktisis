using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Common.Utility;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Ik.TwoJoints;
using Ktisis.Editor.Transforms.Types;
using Ktisis.ImGuizmo;
using Ktisis.Interface.Components.Transforms;
using Ktisis.Interface.Types;
using Ktisis.Scene.Decor.Ik;
using Ktisis.Scene.Entities.Skeleton;
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

		if (target?.Primary is SkeletonNode)
			this.DrawBoneTransformSetup();

		if (target?.Primary is IIkNode ik)
			this.DrawIkSetup(ik);
	}
	
	// Transform table

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
	
	// Transform setup

	private void DrawBoneTransformSetup() {
		ImGui.Spacing();
		if (!ImGui.CollapsingHeader("Bone Transforms")) return;
		ImGui.Spacing();
		
		var cfg = this._ctx.Config.Gizmo;
		ImGui.Checkbox("Bone parenting", ref cfg.ParentBones);
		ImGui.Spacing();
		ImGui.Checkbox("Relative rotation", ref cfg.RelativeBones);
	}
	
	// IK Setup
	// TODO: Clean this up!

	private void DrawIkSetup(IIkNode ik) {
		ImGui.Spacing();
		if (!ImGui.CollapsingHeader("Inverse Kinematics")) return;
		ImGui.Spacing();

		var enable = ik.IsEnabled;
		if (ImGui.Checkbox("Enable IK constraints", ref enable))
			ik.Toggle();

		if (!ik.IsEnabled) return;

		switch (ik) {
			case ITwoJointsNode node:
				this.DrawTwoJoints(node);
				break;
			case ICcdNode node:
				this.DrawCcd(node);
				break;
		}
		
		ImGui.Spacing();

		if (Buttons.IconButton(FontAwesomeIcon.EllipsisH))
			ImGui.OpenPopup("##IkAdvancedCfg");
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);
		ImGui.Text("Advanced parameters");
	}
	
	// CCD

	private void DrawCcd(ICcdNode ik) {
		if (ImGui.IsPopupOpen("##IkAdvancedCfg"))
			this.DrawCcdAdvanced(ik);
	}

	private void DrawCcdAdvanced(ICcdNode ik) {
		using var popup = ImRaii.Popup("##IkAdvancedCfg");
		if (!popup.Success) return;
		
		ImGui.Spacing();
		
		ImGui.SliderFloat("Gain", ref ik.Group.Gain, 0.0f, 1.0f, "%.2f");
		ImGui.SliderInt("Iterations", ref ik.Group.Iterations, 0, 60);
		
		ImGui.Spacing();
	}
	
	// Two Joints

	private void DrawTwoJoints(ITwoJointsNode ik) {
		ImGui.Spacing();
		ImGui.Checkbox("Enforce end rotation", ref ik.Group.EnforceRotation);
		ImGui.Spacing();
		
		ImGui.Text("Transform mode:");
		DrawMode("Fixed target", TwoJointsMode.Fixed, ik.Group);
		DrawMode("Bone relative", TwoJointsMode.Relative, ik.Group);

		if (ImGui.IsPopupOpen("##IkAdvancedCfg"))
			this.DrawTwoJointsAdvanced(ik);
	}

	private void DrawTwoJointsAdvanced(ITwoJointsNode ik) {
		using var popup = ImRaii.Popup("##IkAdvancedCfg");
		if (!popup.Success) return;
		
		ImGui.Spacing();

		ImGui.Text("Gain:");
		ImGui.Spacing();
		ImGui.SliderFloat("Shoulder##FirstWeight", ref ik.Group.FirstBoneGain, 0.0f, 1.0f, "%.2f");
		ImGui.SliderFloat("Elbow##SecondWeight", ref ik.Group.SecondBoneGain, 0.0f, 1.0f, "%.2f");
		ImGui.SliderFloat("Hand##HandWeight", ref ik.Group.EndBoneGain, 0.0f, 1.0f, "%.2f");
		
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		
		ImGui.Text("Hinges:");
		ImGui.Spacing();
		ImGui.SliderFloat("Minimum", ref ik.Group.MinHingeAngle, -1.0f, 1.0f, "%.2f");
		ImGui.SliderFloat("Maximum", ref ik.Group.MaxHingeAngle, -1.0f, 1.0f, "%.2f");
		ImGui.SliderFloat3("Axis", ref ik.Group.HingeAxis, -1.0f, 1.0f, "%.2f");
		
		ImGui.Spacing();
	}

	private static void DrawMode(string label, TwoJointsMode mode, TwoJointsGroup group) {
		var value = group.Mode == mode;
		if (ImGui.RadioButton(label, value))
			group.Mode = mode;
	}
}
