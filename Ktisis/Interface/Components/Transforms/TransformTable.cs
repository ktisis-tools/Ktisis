using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using Ktisis.Common.Utility;
using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Config.Sections;
using Ktisis.Editor.Selection;
using Ktisis.Localization;

using Hexa.NET.ImGuizmo;

namespace Ktisis.Interface.Components.Transforms;

[Flags]
public enum TransformTableFlags {
	None = 0,
	Position = 1,
	Rotation = 2,
	Scale = 4,
	Operation = 8,
	UseAvailable = 16,
	Default = Position | Rotation | Scale | Operation
}

[Transient]
public class TransformTable {
	private readonly ConfigManager _cfg;
	private readonly LocaleManager _locale;

	private GizmoConfig GizmoConfig => this._cfg.File.Gizmo;

	public TransformTable(
		ConfigManager cfg,
		LocaleManager locale
	) {
		this._cfg = cfg;
		this._locale = locale;
	}
	
	// State

	private bool IsUsed;
	
	public bool IsActive { get; private set; }
	public bool IsDeactivated { get; private set; }

	private Vector3 Angles = Vector3.Zero;
	private Quaternion Value = Quaternion.Identity;

	private const ImGuizmoOperation PositionOp = ImGuizmoOperation.Translate;
	private const ImGuizmoOperation RotateOp = ImGuizmoOperation.Rotate;
	private const ImGuizmoOperation ScaleOp = ImGuizmoOperation.Scale | ImGuizmoOperation.Scaleu;

	private Transform Transform = new();
	
	// Draw UI

	private readonly static Vector3 MinScale = new(0.1f, 0.1f, 0.1f);

	private static uint[] AxisColors = [
		0xFF3553FF,
		0xFF00D154,
		0xFFFF5400
	];

	public bool Draw(Transform transIn, out Transform transOut, TransformTableFlags flags = TransformTableFlags.Default) {
		using var _ = ImRaii.PushId($"TransformTable_{this.GetHashCode():X}");

		if (!this.IsActive && !transIn.Rotation.Equals(this.Value)) {
			this.Angles = HkaEulerAngles.ToEuler(transIn.Rotation);
			this.Value = transIn.Rotation;
		}
		
		this.IsUsed = false;
		this.IsActive = false;
		this.IsDeactivated = false;

		try {
			var useAvail = flags.HasFlag(TransformTableFlags.UseAvailable);
			ImGui.PushItemWidth(useAvail ? CalcTableAvail() : CalcTableWidth());

			var op = flags.HasFlag(TransformTableFlags.Operation);
			transOut = this.Transform.Set(transIn);
			if (flags.HasFlag(TransformTableFlags.Position))
				this.DrawPosition(ref transOut.Position, op);
			if (flags.HasFlag(TransformTableFlags.Rotation))
				this.DrawRotate(ref transOut.Rotation, op);
			if (flags.HasFlag(TransformTableFlags.Scale) && this.DrawScale(ref transOut.Scale, op))
				transOut.Scale = Vector3.Max(transOut.Scale, MinScale);
		} finally {
			ImGui.PopItemWidth();
		}

		return this.IsUsed;
	}

	public bool DrawPosition(ref Vector3 position, TransformTableFlags flags = TransformTableFlags.Default) {
		using var _ = ImRaii.PushId($"TransformTable_{this.GetHashCode():X}");
		this.IsUsed = false;
		this.IsDeactivated = false;
		try {
			var useAvail = flags.HasFlag(TransformTableFlags.UseAvailable);
			ImGui.PushItemWidth(useAvail ? ImGui.GetContentRegionAvail().X : CalcTableWidth());
			var operation = flags.HasFlag(TransformTableFlags.Operation);
			this.DrawPosition(ref position, operation);
		} finally {
			ImGui.PopItemWidth();
		}
		return this.IsUsed;
	}
	
	// Individual transforms

	private bool DrawPosition(ref Vector3 pos, bool op) {
		var result = this.DrawLinear("##TransformTable_Pos", ref pos);
		if (op) this.DrawOperation(PositionOp, FontAwesomeIcon.LocationArrow, "transform.position");
		return result;
	}

	private bool DrawRotate(ref Quaternion rot, bool op) {
		var result = this.DrawEuler("##TransformTable_Rotate", ref this.Angles);
		if (result) {
			rot = HkaEulerAngles.ToQuaternion(this.Angles);
			this.Value = rot;
		}
		if (op) this.DrawOperation(RotateOp, FontAwesomeIcon.ArrowsSpin, "transform.rotation");
		return result;
	}

	private bool DrawScale(ref Vector3 scale, bool op) {
		var result = this.DrawLinear("##TransformTable_Scale", ref scale);
		if (op) this.DrawOperation(ScaleOp, FontAwesomeIcon.Expand, "transform.scale");
		return result;
	}

	private void DrawOperation(ImGuizmoOperation op, FontAwesomeIcon icon, string hint) {
		var spacing = ImGui.GetStyle().ItemSpacing.X;
		ImGui.SameLine(0, spacing);

		var enable = this.GizmoConfig.Operation.HasFlag(op) ? 0xFFFFFFFF : 0xAFFFFFFF;
		ImGui.PushStyleColor(ImGuiCol.Text, enable);
		if (Buttons.IconButtonTooltip(icon, this._locale.Translate(hint)))
			this.ChangeOperation(op);
		ImGui.PopStyleColor();
	}

	private void ChangeOperation(ImGuizmoOperation op) {
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

	private bool DrawEuler(string id, ref Vector3 vec) {
		var used = this.DrawXYZ(id, ref vec, 0.2f);
		if (used) vec = vec.NormalizeAngles();
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
		var result = ImGui.DragFloat(id, ref value, speed, 0, 0, "%.3f", ImGuiSliderFlags.NoRoundToFormat);
		ImGui.PopStyleColor();
		ImGui.PopStyleVar(2);
		this.IsActive |= ImGui.IsItemActive();
		this.IsDeactivated |= ImGui.IsItemDeactivatedAfterEdit();
		return result;
	}
	
	// Space calculations

	private static float CalcTableAvail()
		=> ImGui.GetContentRegionAvail().X - CalcIconSpacing();

	private static float CalcTableWidth()
		=> UiBuilder.DefaultFont.FontSize * 4.00f * 3;

	private static float CalcIconSpacing()
		=> UiBuilder.IconFont.FontSize + ImGui.GetStyle().ItemSpacing.X * 2;
	
	public static float CalcWidth()
		=> CalcTableWidth() + CalcIconSpacing();
}
