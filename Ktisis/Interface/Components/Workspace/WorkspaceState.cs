using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using ImGuiNET;

using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Transforms;
using Ktisis.Interface.Widgets;

namespace Ktisis.Interface.Components.Workspace;

public class WorkspaceState {
	private readonly IEditorContext _ctx;
	
	public WorkspaceState(
		IEditorContext ctx
	) {
		this._ctx = ctx;
	}

	public void Draw() {
		var style = ImGui.GetStyle();
		var height = (ImGui.GetFontSize() + style.ItemInnerSpacing.Y) * 2 + style.ItemSpacing.Y;

		var frame = false;
		try {
			var id = ImGui.GetID("SceneState_Frame");
			frame = ImGui.BeginChildFrame(id, new Vector2(-1, height));
			if (!frame) return;
			this.DrawContext();
		} finally {
			if (frame) ImGui.EndChildFrame();
		}
	}

	private void DrawContext() {
		var cursorY = ImGui.GetCursorPosY();
		var avail = ImGui.GetContentRegionAvail().Y;
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemSpacing.X);
		ImGui.SetCursorPosY(cursorY + (avail - ImGui.GetFrameHeight()) / 2);
		
		var isPosing = this._ctx.Posing.IsEnabled;
		
		var locKey = isPosing ? "enable" : "disable";
		
		var color = isPosing ? 0xFF3AD86A : 0xFF504EC4;
		using var button = ImRaii.PushColor(ImGuiCol.Button, isPosing ? 0xFF00FF00 : 0xFF7070C0);
		if (ToggleButton.Draw("##KtisisPoseToggle", ref isPosing, color))
			this._ctx.Posing.SetEnabled(isPosing);

		if (ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text(this._ctx.Locale.Translate($"workspace.posing.hint.{locKey}"));
		}

		ImGui.SameLine();

		var style = ImGui.GetStyle();
		var labelHeight = UiBuilder.IconFont.FontSize * 2 + style.ItemInnerSpacing.Y;
		ImGui.SetCursorPosY(cursorY + (avail - labelHeight) / 2);
		ImGui.BeginGroup();
		
		using (var space = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
			using (var text = ImRaii.PushColor(ImGuiCol.Text, color))
				ImGui.Text(this._ctx.Locale.Translate($"workspace.posing.toggle.{locKey}"));
			
			using (var text = ImRaii.PushColor(ImGuiCol.Text, 0xDFFFFFFF))
				this.DrawTargetLabel(this._ctx.Transform);
		}
		
		ImGui.EndGroup();
	}

	private void DrawTargetLabel(ITransformHandler transform) {
		var target = transform.Target;
		if (target == null) {
			ImGui.TextDisabled(this._ctx.Locale.Translate("workspace.state.select_count.none"));
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
		ImGui.Text(this._ctx.Locale.Translate(
			key,
			new Dictionary<string, string> {
				{ "count", count.ToString() },
				{ "target", target.Primary?.Name ?? "INVALID" }
			}
		));
	}
}
