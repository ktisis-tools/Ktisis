using System.Numerics;

using Dalamud.Interface;

using ImGuiNET;

using Ktisis.ImGuizmo;
using Ktisis.Common.Utility;
using Ktisis.Common.Extensions;
using Ktisis.Interface.Widgets;
using Ktisis.Editing;
using Ktisis.Localization;

namespace Ktisis.Interface.Components;

public delegate void OnClickOperationHandler(Operation op);

public class TransformTable {
	// Constructor

	private readonly string Id;

	private readonly LocaleService _locale;

	public TransformTable(string id, LocaleService _locale) {
		this.Id = id;

		this._locale = _locale;
	}

	// State

	private bool IsUsed;

	private Vector3 Angles;

	public Operation Operation = Operation.ROTATE;

	private const Operation PositionOp = Operation.TRANSLATE;
	private const Operation RotateOp = Operation.ROTATE;
	private const Operation ScaleOp = Operation.SCALE | Operation.SCALE_U;

	// Events

	public OnClickOperationHandler? OnClickOperation;

	// UI draw

	private readonly static Vector3 MinScale = new(0.1f, 0.1f, 0.1f);

	private static uint[] AxisColors = {
		0xFF3553FF,
		0xFF00D154,
		0xFFFF5400
	};

	public bool Draw(ref Transform trans) {
		if (!ImGui.IsItemActive())
			this.Angles = trans.Rotation.ToEulerAngles();

		ImGui.PushItemWidth(CalcTableWidth());

		DrawPosition(ref trans.Position);
		DrawRotate(ref trans.Rotation);
		if (DrawScale(ref trans.Scale))
			trans.Scale = Vector3.Max(trans.Scale, MinScale);

		ImGui.PopItemWidth();

		return this.IsUsed;
	}

	// Individual transforms

	private bool DrawPosition(ref Vector3 pos) {
		var result = DrawLinear($"{this.Id}_Pos", ref pos);
		DrawOperation(PositionOp, FontAwesomeIcon.LocationArrow, this._locale.Translate("transform.position"));
		return result;
	}

	private bool DrawRotate(ref Quaternion rot) {
		var result = DrawEuler($"{this.Id}_Rotate", ref this.Angles, out var delta);
		if (result) rot *= delta.EulerAnglesToQuaternion();
		DrawOperation(RotateOp, FontAwesomeIcon.ArrowsSpin, this._locale.Translate("transform.rotation"));
		return result;
	}

	private bool DrawScale(ref Vector3 scale) {
		var result = DrawLinear($"{this.Id}_Scale", ref scale);
		DrawOperation(ScaleOp, FontAwesomeIcon.Expand, this._locale.Translate("transform.scale"));
		return result;
	}

	private void DrawOperation(Operation op, FontAwesomeIcon icon, string hint) {
		var spacing = ImGui.GetStyle().ItemSpacing.X;
		if (this.OnClickOperation != null) {
			ImGui.SameLine(0, spacing);

			var enable = this.Operation.HasFlag(op) ? 0xFFFFFFFF : 0xAFFFFFFF;
			ImGui.PushStyleColor(ImGuiCol.Text, enable);
			if (Buttons.DrawIconButtonHint(icon, hint))
				ChangeOperation(op);
			ImGui.PopStyleColor();
		} else {
			var space = UiBuilder.IconFont.FontSize;
			var size = Icons.CalcIconSize(icon).X;
			ImGui.SameLine(0, spacing + (size) - (space / 2));
			Icons.DrawIcon(icon);
		}
	}

	private void ChangeOperation(Operation op) {
		if (GuiHelpers.GetSelectFlags().HasFlag(SelectFlags.Ctrl))
			this.Operation |= op;
		else
			this.Operation = op;

		this.OnClickOperation?.Invoke(this.Operation);
	}

	// Drag

	private bool DrawLinear(string id, ref Vector3 vec) {
		var used = DrawXYZ(id, ref vec, 0.001f);
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
		result |= DrawColFloat($"{id}_X", ref vec.X, speed, AxisColors[0]);
		ImGui.SameLine(0, spacing);
		result |= DrawColFloat($"{id}_Y", ref vec.Y, speed, AxisColors[1]);
		ImGui.SameLine(0, spacing);
		result |= DrawColFloat($"{id}_Z", ref vec.Z, speed, AxisColors[2]);
		ImGui.PopItemWidth();
		return result;
	}

	private bool DrawColFloat(string id, ref float value, float speed, uint col) {
		ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding.Add(0.1f));
		ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0.1f);
		ImGui.PushStyleColor(ImGuiCol.Border, col);

		var result = ImGui.DragFloat(id, ref value, speed, -360, 360, "%.3f", ImGuiSliderFlags.NoRoundToFormat);

		ImGui.PopStyleColor();
		ImGui.PopStyleVar(2);
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
