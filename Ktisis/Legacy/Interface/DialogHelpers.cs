using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs;

using GLib.Widgets;

namespace Ktisis.Legacy.Interface;

public static class DialogHelpers {
	public static void BuildDialog(ref bool newSet, bool newDefault, string tooltipString, string newSettingName, string secondaryText) {
		ImGui.AlignTextToFramePadding();
		ImGui.Text(newSettingName);
		if(tooltipString!= string.Empty) //this._migrator.Locale.Translate(newSettingName)
			DrawHint(tooltipString);
		var defaultText = $"Default: {(newDefault ? "On" : "Off")}";
		ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() - ImGui.CalcTextSize(defaultText).X - ImGui.GetStyle().FramePadding.X);
		ImGui.TextDisabled(defaultText);
		ImGui.SameLine();
		ImGui.Checkbox($"##{newSettingName}", ref newSet);

		if (secondaryText != string.Empty) {
			ImGui.Indent();
			using (ImRaii.TextWrapPos(ImGui.GetContentRegionMax().X * .65f)) {
				ImGui.TextWrapped(secondaryText);
			}
			ImGui.Unindent();
		}
	}
	
	public static void BuildDialog(ref float newSet, float newDefault, string tooltipString, string newSettingName, string secondaryText) {
		ImGui.AlignTextToFramePadding();
		ImGui.Text(newSettingName);
		if(tooltipString != string.Empty)
			DrawHint(tooltipString);//this._migrator.Locale.Translate(newSettingName)
		var defaultText = $"Default: {newDefault}";
		ImGui.SameLine(ImGui.GetContentRegionAvail().X - 80f - ImGui.CalcTextSize(defaultText).X - ImGui.GetStyle().FramePadding.X);
		ImGui.TextDisabled(defaultText);
		ImGui.SameLine();
		ImGui.PushItemWidth(80f);
		ImGui.InputFloat($"##{newSettingName}", ref newSet);
		ImGui.PopItemWidth();

		if (secondaryText != string.Empty) {
			ImGui.Indent();
			using (ImRaii.TextWrapPos(ImGui.GetContentRegionMax().X * .65f)) {
				ImGui.TextWrapped(secondaryText);
			}
		
			ImGui.Unindent();
		}
	}
	
	public static void DrawHint(string tooltipString) {
		ImGui.SameLine();
		Icons.DrawIcon(FontAwesomeIcon.QuestionCircle);
		if (ImGui.IsItemHovered()) {
			using var _ = ImRaii.Tooltip();
			ImGui.Text(tooltipString);
		}
	}
}
