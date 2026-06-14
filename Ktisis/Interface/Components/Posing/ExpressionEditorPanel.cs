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
	public IExpressionEditor Editor { set; private get; } = null!;

	private string _captureId = string.Empty;
	private string _captureLabel = string.Empty;

	// Snapshot taken at the start of a slider drag, committed as an undo memento
	// when the drag ends.
	private PoseContainer? _editInitial;

	// Deferred so we don't mutate the catalog while enumerating it.
	private string? _pendingDelete;

	public void Draw() {
		if (this.Editor == null) return;
		this.Editor.EnsureNeutral();

		this.DrawToolbar();
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();

		foreach (var group in this.Editor.Catalog.Groups) {
			if (group.Units.Count == 0) continue;

			Separators.SeparatorText(group.Name, textColor: ImGui.GetColorU32(ImGuiCol.Header));

			using var _id = ImRaii.PushId(group.Name);
			foreach (var unit in group.Units)
				this.DrawUnitSlider(unit);
		}

		if (this._pendingDelete != null) {
			this.Editor.RemoveUnit(this._pendingDelete);
			this._pendingDelete = null;
		}
	}

	private void DrawUnitSlider(ActionUnit unit) {
		using var _id = ImRaii.PushId(unit.Id);

		if (Buttons.IconButtonTooltip(FontAwesomeIcon.TrashAlt, $"Delete '{unit.Label}'", default))
			this._pendingDelete = unit.Id;
		ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

		var weight = this.Editor.GetWeight(unit.Id);
		var min = unit.Bidirectional ? -1.0f : 0.0f;

		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
		if (ImGui.SliderFloat(unit.Label, ref weight, min, 1.0f)) {
			this._editInitial ??= this.Editor.BeginEdit();
			this.Editor.SetWeight(unit.Id, weight);
		}

		if (ImGui.IsItemDeactivatedAfterEdit() && this._editInitial != null) {
			this.Editor.CommitEdit(this._editInitial);
			this._editInitial = null;
		}
	}

	private void DrawToolbar() {
		if (ImGui.Button("Reset All")) {
			var initial = this.Editor.BeginEdit();
			this.Editor.ResetWeights();
			this.Editor.CommitEdit(initial);
		}

		ImGui.SameLine();
		if (ImGui.Button("Recapture Neutral"))
			this.Editor.CaptureNeutral();

		ImGui.SameLine();
		if (ImGui.Button("Capture as AU"))
			ImGui.OpenPopup("##capture_au");

		this.DrawCapturePopup();
	}

	private void DrawCapturePopup() {
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
				this.Editor.CaptureCurrentAsAu(this._captureId, label);
				this._captureId = string.Empty;
				this._captureLabel = string.Empty;
				ImGui.CloseCurrentPopup();
			}
		}
	}
}
