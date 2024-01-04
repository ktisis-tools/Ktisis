using System.Numerics;

using Dalamud.Interface;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.ImGuizmo;
using Ktisis.Core.Attributes;
using Ktisis.Common.Utility;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Selection;

namespace Ktisis.Interface.Components.Transforms;

[Transient]
public class TransformTable {
	private readonly ConfigManager _cfg;

	private GizmoConfig GizmoConfig => this._cfg.Config.Gizmo;
	
	public TransformTable(
		ConfigManager cfg
	) {
		this._cfg = cfg;
	}
	
	// State

	private bool IsUsed;
	
	public bool IsDeactivated { get; private set; }

	private Vector3 Angles;

	private const Operation PositionOp = Operation.TRANSLATE;
	private const Operation RotateOp = Operation.ROTATE;
	private const Operation ScaleOp = Operation.SCALE | Operation.SCALE_U;

	private Transform Transform = new();
	
	// Draw UI

	private static readonly Vector3 MinScale = new(0.1f, 0.1f, 0.1f);

	private static uint[] AxisColors = [
		0xFF3553FF,
		0xFF00D154,
		0xFFFF5400
	];

	public bool Draw(Transform transIn, out Transform transOut) {
		if (!this.IsUsed)
			this.Angles = transIn.Rotation.ToEulerAngles();

		this.IsUsed = false;
		this.IsDeactivated = false;

		try {
			ImGui.PushItemWidth(CalcTableWidth());
			
			transOut = this.Transform.Set(transIn);
			this.DrawPosition(ref transOut.Position);
			this.DrawRotate(ref transOut.Rotation);
			if (this.DrawScale(ref transOut.Scale))
				transOut.Scale = Vector3.Max(transOut.Scale, MinScale);
		} finally {
			ImGui.PopItemWidth();
		}

		return this.IsUsed;
	}
	
	// Individual transforms

	private bool DrawPosition(ref Vector3 pos) {
		var result = DrawLinear("##TransformTable_Pos", ref pos);
		this.DrawOperation(PositionOp, FontAwesomeIcon.LocationArrow, "transform.position");
		return result;
	}

	private bool DrawRotate(ref Quaternion rot) {
		var result = DrawEuler("##TransformTable_Rotate", ref this.Angles, out var delta);
		if (result) rot *= delta.EulerAnglesToQuaternion();
		this.DrawOperation(RotateOp, FontAwesomeIcon.ArrowsSpin, "transform.rotation");
		return result;
	}

	private bool DrawScale(ref Vector3 scale) {
		var result = DrawLinear("##TransformTable_Scale", ref scale);
		this.DrawOperation(ScaleOp, FontAwesomeIcon.Expand, "transform.scale");
		return result;
	}

	private void DrawOperation(Operation op, FontAwesomeIcon icon, string hint) {
		var spacing = ImGui.GetStyle().ItemSpacing.X;
		//if (this.OnClickOperation != null) {
			ImGui.SameLine(0, spacing);

			var enable = this.GizmoConfig.Operation.HasFlag(op) ? 0xFFFFFFFF : 0xAFFFFFFF;
			ImGui.PushStyleColor(ImGuiCol.Text, enable);
			if (Buttons.IconButtonTooltip(icon, hint))
				this.ChangeOperation(op);
			ImGui.PopStyleColor();
		/*} else {
			var space = UiBuilder.IconFont.FontSize;
			var size = Icons.CalcIconSize(icon).X;
			ImGui.SameLine(0, spacing + (size) - (space / 2));
			Icons.DrawIcon(icon);
		}*/
	}

	private void ChangeOperation(Operation op) {
		if (GuiHelpers.GetSelectMode() == SelectMode.Multiple)
			this.GizmoConfig.Operation |= op;
		else
			this.GizmoConfig.Operation = op;
	}
	
	// Drag

	private bool DrawLinear(string id, ref Vector3 vec) {
		var used = this.DrawXYZ(id, ref vec, 0.001f);
		this.IsUsed |= used;
		return used;
	}

	private bool DrawEuler(string id, ref Vector3 vec, out Vector3 delta) {
		var result = vec;
		delta = Vector3.Zero;
		
		var used = DrawXYZ(id, ref result, 0.2f);
		if (used) {
			delta = result - vec;
			vec = result.NormalizeAngles();
		}
		
		this.IsUsed |= used;
		return used;
	}
	
	// Individual components

	private bool DrawXYZ(string id, ref Vector3 vec, float speed) {
		var result = false;
		var spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		ImGui.PushItemWidth((ImGui.CalcItemWidth() - spacing * 2) / 3);
		result |= this.DrawAxis($"{id}_X", ref vec.X, speed, AxisColors[0]);
		ImGui.SameLine(0, spacing);
		result |= this.DrawAxis($"{id}_Y", ref vec.Y, speed, AxisColors[1]);
		ImGui.SameLine(0, spacing);
		result |= this.DrawAxis($"{id}_Z", ref vec.Z, speed, AxisColors[2]);
		ImGui.PopItemWidth();
		return result;
	}

	private bool DrawAxis(string id, ref float value, float speed, uint col) {
		ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding + new Vector2(0.1f, 0.1f));
		ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.1f);
		ImGui.PushStyleColor(ImGuiCol.Border, col);
		var result = ImGui.DragFloat(id, ref value, speed, -360, 360, "%.3f", ImGuiSliderFlags.NoRoundToFormat);
		ImGui.PopStyleColor();
		ImGui.PopStyleVar(2);
		this.IsDeactivated |= ImGui.IsItemDeactivatedAfterEdit();
		return result;
	}
	
	// Space calculations
	
	private static float CalcTableWidth()
		=> UiBuilder.DefaultFont.FontSize * 3.65f * 3;

	private static float CalcIconSpacing()
		=> UiBuilder.IconFont.FontSize + ImGui.GetStyle().ItemSpacing.X * 2;

	public static float CalcWidth()
		=> CalcTableWidth() + CalcIconSpacing();
}
