using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Expressions.Data;
using Ktisis.Editor.Expressions.Types;
using Ktisis.Localization;

namespace Ktisis.Interface.Components.Posing;

[Transient]
public class ExpressionEditorPanel {
	private readonly LocaleManager _locale;

	private Dictionary<string, float>? _editInitial;
	private readonly HashSet<string> _unlinked = new();

	private readonly static IReadOnlyList<(string Left, string? Right)> Face = [
		("BrowUpL", "BrowUpR"),
		("BrowFurrowL", "BrowFurrowR"),
		("BlinkL", "BlinkR"),
		("EyeWideL", "EyeWideR"),
		("CheekRaiseL", "CheekRaiseR"),
		("SmileL", "SmileR"),
		("FrownL", "FrownR"),
		("SneerL", "SneerR"),
		("JawOpen", null),
		("UpperLipOpen", null),
		("LowerLipOpen", null),
		("LipPucker", null),
	];
	
	public ExpressionEditorPanel(LocaleManager locale) {
		this._locale = locale;
	}

	private static float GutterWidth => Buttons.CalcSize() + ImGui.GetStyle().ItemSpacing.X * 2;

	public void Draw(IExpressionEditor editor) {
		editor.EnsureNeutral();

		this.DrawToolbar(editor);
		ImGui.Spacing();

		Separators.SeparatorText(this._locale.Translate("expression.group.Face"), textColor: ImGui.GetColorU32(ImGuiCol.Header));

		foreach (var (leftId, rightId) in Face) {
			var left = editor.Catalog.FindUnit(leftId);
			var right = rightId != null ? editor.Catalog.FindUnit(rightId) : null;

			if (left != null && right != null) {
				this.DrawUnitPair(editor, left, right);
			} else if (left is {} single) {
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + GutterWidth);
				this.DrawUnitSlider(editor, single);
			}
		}
	}

	private void DrawUnitPair(IExpressionEditor editor, ActionUnit left, ActionUnit right) {
		using var _ = ImRaii.PushId(left.Id);

		var linked = !this._unlinked.Contains(left.Id);
		var gutterX = ImGui.GetCursorScreenPos().X;

		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + GutterWidth);
		this.DrawUnitSlider(editor, left, linked ? right.Id : null);
		var leftMin = ImGui.GetItemRectMin();

		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + GutterWidth);
		this.DrawUnitSlider(editor, right, linked ? left.Id : null);
		var rightMin = ImGui.GetItemRectMin();

		this.DrawPairLock(left.Id, linked, gutterX, leftMin, rightMin);
	}

	private void DrawPairLock(string pairId, bool linked, float gutterX, Vector2 leftMin, Vector2 rightMin) {
		var halfFrame = ImGui.GetFrameHeight() / 2;
		var leftMidY = leftMin.Y + halfFrame;
		var rightMidY = rightMin.Y + halfFrame;

		var size = Buttons.CalcSize();
		var btnPos = new Vector2(gutterX, (leftMidY + rightMidY - size) / 2);

		var restore = ImGui.GetCursorPos();
		ImGui.SetCursorScreenPos(btnPos);
		if (Buttons.IconButtonTooltip(
			linked ? FontAwesomeIcon.Lock : FontAwesomeIcon.Unlock,
			this._locale.Translate(linked ? "expression.unlink" : "expression.link"),
			iconColor: linked ? null : ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]
		)) {
			if (linked) this._unlinked.Add(pairId);
			else this._unlinked.Remove(pairId);
		}
		ImGui.SetCursorPos(restore);

		var midX = btnPos.X + size / 2;
		var endX = leftMin.X - 2;
		var color = ImGui.GetColorU32(ImGuiCol.Text, linked ? 0.5f : 0.2f);

		var drawList = ImGui.GetWindowDrawList();
		drawList.AddBezierCubic(btnPos with { X = midX }, new(midX, leftMidY), new(midX, leftMidY), new(endX, leftMidY), color, 1.0f);
		drawList.AddBezierCubic(new(midX, btnPos.Y + size), new(midX, rightMidY), new(midX, rightMidY), new(endX, rightMidY), color, 1.0f);
	}

	private void DrawUnitSlider(IExpressionEditor editor, ActionUnit unit, string? linkedId = null) {
		using var _ = ImRaii.PushId(unit.Id);

		var weight = editor.GetWeight(unit.Id);
		var min = unit.Bidirectional ? -1.0f : 0.0f;

		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
		if (ImGui.SliderFloat(this._locale.Translate($"expression.unit.{unit.Id}", fallback: unit.Label), ref weight, min, 1.0f)) {
			this._editInitial ??= editor.BeginEdit();
			editor.SetWeight(unit.Id, weight);
			if (linkedId != null)
				editor.SetWeight(linkedId, weight);
		}

		if (ImGui.IsItemDeactivatedAfterEdit() && this._editInitial != null) {
			editor.CommitEdit(this._editInitial);
			this._editInitial = null;
		}
	}

	private void DrawToolbar(IExpressionEditor editor) {
		if (!ImGui.Button(this._locale.Translate("expression.reset_all")))
			return;

		var initial = editor.BeginEdit();
		editor.ResetWeights();
		editor.CommitEdit(initial);
	}
}
