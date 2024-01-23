using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Core.Attributes;
using Ktisis.Editor.Context;
using Ktisis.Editor.Transforms;
using Ktisis.Interface.Widgets;
using Ktisis.Localization;

namespace Ktisis.Interface.Components.Workspace;

[Transient]
public class WorkspaceState {
	private readonly LocaleManager _locale;
	
	public WorkspaceState(
		LocaleManager locale
	) {
		this._locale = locale;
	}

	public void Draw(IEditorContext? context) {
		var style = ImGui.GetStyle();
		var height = (ImGui.GetFontSize() + style.ItemInnerSpacing.Y) * 2 + style.ItemSpacing.Y;

		var frame = false;
		try {
			var id = ImGui.GetID("SceneState_Frame");
			frame = ImGui.BeginChildFrame(id, new Vector2(-1, height));
			if (!frame) return;
			
			if (context != null)
				this.DrawContext(context);
			else
				ImGui.Text("Inactive.");
		} finally {
			if (frame) ImGui.EndChildFrame();
		}
	}

	private void DrawContext(IEditorContext context) {
		var cursorY = ImGui.GetCursorPosY();
		var avail = ImGui.GetContentRegionAvail().Y;
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemSpacing.X);
		ImGui.SetCursorPosY(cursorY + (avail - ImGui.GetFrameHeight()) / 2);
		
		var isPosing = context.Posing.IsEnabled;
		
		var locKey = isPosing ? "enable" : "disable";
		
		var color = isPosing ? 0xFF3AD86A : 0xFF504EC4;
		using var _button = ImRaii.PushColor(ImGuiCol.Button, isPosing ? 0xFF00FF00 : 0xFF7070C0);
		if (ToggleButton.Draw("##KtisisPoseToggle", ref isPosing, color))
			context.Posing.SetEnabled(isPosing);

		if (ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text(this._locale.Translate($"workspace.posing.hint.{locKey}"));
		}

		ImGui.SameLine();

		var style = ImGui.GetStyle();
		var labelHeight = UiBuilder.IconFont.FontSize * 2 + style.ItemInnerSpacing.Y;
		ImGui.SetCursorPosY(cursorY + (avail - labelHeight) / 2);
		ImGui.BeginGroup();
		
		using (var _space = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
			using (var _text = ImRaii.PushColor(ImGuiCol.Text, color))
				ImGui.Text(this._locale.Translate($"workspace.posing.toggle.{locKey}"));
			
			using (var _text = ImRaii.PushColor(ImGuiCol.Text, 0xDFFFFFFF))
				this.DrawTargetLabel(context.Transform);
		}
		
		ImGui.EndGroup();
	}

	private void DrawTargetLabel(ITransformHandler transform) {
		var target = transform.Target;
		if (target == null) {
			ImGui.TextDisabled(this._locale.Translate("workspace.state.select_count.none"));
			return;
		}

		var name = target.Primary?.Name ?? "INVALID";

		var count = transform.Target!.Targets.Count();
		if (count == 1) {
			ImGui.Text(name);
			return;
		}

		count--;
		
		var key = $"workspace.state.select_count.{(count > 1 ? "plural" : "single")}";
		ImGui.Text(this._locale.Translate(
			key,
			new() {
				{ "count", count.ToString() },
				{ "target", target.Primary?.Name ?? "INVALID" }
			}
		));
	}
}
