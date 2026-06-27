using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Expressions.Data;
using Ktisis.Editor.Expressions.Types;
using Ktisis.Editor.Posing.Data;

namespace Ktisis.Interface.Components.Posing;

[Transient]
public class ExpressionEditorPanel {

	private string _captureId = string.Empty;
	private string _captureLabel = string.Empty;

	private PoseContainer? _editInitial;
	private string? _pendingDelete;

	public void Draw(IExpressionEditor editor) {
		editor.EnsureNeutral();

		this.DrawToolbar(editor);
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		foreach (var group in editor.Catalog.Groups) {
			if (group.Units.Count == 0) continue;

			Separators.SeparatorText(group.Name, textColor: ImGui.GetColorU32(ImGuiCol.Header));

			using var _id = ImRaii.PushId(group.Name);
			foreach (var unit in group.Units)
				this.DrawUnitSlider(editor, unit);
		}

		if (this._pendingDelete != null) {
			editor.RemoveUnit(this._pendingDelete);
			this._pendingDelete = null;
		}
	}

	private void DrawUnitSlider(IExpressionEditor editor, ActionUnit unit) {
		using var id = ImRaii.PushId(unit.Id);

		if (Buttons.IconButtonTooltip(FontAwesomeIcon.TrashAlt, $"Delete '{unit.Label}'"))
			this._pendingDelete = unit.Id;
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

		var weight = editor.GetWeight(unit.Id);
		var min = unit.Bidirectional ? -1.0f : 0.0f;

		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
		if (ImGui.SliderFloat(unit.Label, ref weight, min, 1.0f)) {
			this._editInitial ??= editor.BeginEdit();
			editor.SetWeight(unit.Id, weight);
		}

		if (ImGui.IsItemDeactivatedAfterEdit() && this._editInitial != null) {
			editor.CommitEdit(this._editInitial);
			this._editInitial = null;
		}
	}

	private void DrawToolbar(IExpressionEditor editor) {
		if (ImGui.Button("Reset All")) {
			var initial = editor.BeginEdit();
			editor.ResetWeights();
			editor.CommitEdit(initial);
		}
#if DEBUG
		ImGui.SameLine();
		if (ImGui.Button("Recapture Neutral"))
			editor.CaptureNeutral();

		ImGui.SameLine();
		if (ImGui.Button("Capture as AU"))
			ImGui.OpenPopup("##capture_au");

		this.DrawCapturePopup(editor);
#endif
	}

	private void DrawCapturePopup(IExpressionEditor editor) {
		using var popup = ImRaii.Popup("##capture_au");
		if (!popup.Success) return;

		ImGui.Text("Capture the current face as a new Action Unit.");
		ImGui.Spacing();

		ImGui.InputText("Id", ref this._captureId, 64);
		ImGui.InputText("Label", ref this._captureLabel, 64);

		ImGui.Spacing();
		using (ImRaii.Disabled(string.IsNullOrWhiteSpace(this._captureId))) {
			if (ImGui.Button("Capture")) {
				var label = string.IsNullOrWhiteSpace(this._captureLabel) ? this._captureId : this._captureLabel;
				editor.CaptureCurrentAsAu(this._captureId, label);
				this._captureId = string.Empty;
				this._captureLabel = string.Empty;
				ImGui.CloseCurrentPopup();
			}
		}
	}
}
