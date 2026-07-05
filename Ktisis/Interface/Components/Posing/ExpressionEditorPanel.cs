using System.Collections.Generic;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

using GLib.Widgets;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Expressions.Data;
using Ktisis.Editor.Expressions.Types;

namespace Ktisis.Interface.Components.Posing;

[Transient]
public class ExpressionEditorPanel {

	private Dictionary<string, float>? _editInitial;

	public void Draw(IExpressionEditor editor) {
		editor.EnsureNeutral();

		this.DrawToolbar(editor);
		ImGui.Spacing();

		foreach (var group in editor.Catalog.Groups) {
			if (group.Units.Count == 0) continue;

			Separators.SeparatorText(group.Name, textColor: ImGui.GetColorU32(ImGuiCol.Header));

			using var _ = ImRaii.PushId(group.Name);
			foreach (var unit in group.Units)
				this.DrawUnitSlider(editor, unit);
		}
	}

	private void DrawUnitSlider(IExpressionEditor editor, ActionUnit unit) {
		using var _ = ImRaii.PushId(unit.Id);

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
		if (!ImGui.Button("Reset All"))
			return;
		
		var initial = editor.BeginEdit();
		editor.ResetWeights();
		editor.CommitEdit(initial);
	}
}
