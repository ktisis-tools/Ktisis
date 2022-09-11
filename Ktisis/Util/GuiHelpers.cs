using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.Components;

namespace Ktisis.Util
{
	internal class GuiHelpers
	{
		public static bool IconButtonHoldConfirm(FontAwesomeIcon icon, string tooltip, bool isHoldingKey)
		{
			if (!isHoldingKey) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
			bool accepting = ImGuiComponents.IconButton(icon);
			if (!isHoldingKey) ImGui.PopStyleVar();

			Tooltip(tooltip);

			return accepting && isHoldingKey;
		}
		public static bool IconButtonTooltip(FontAwesomeIcon icon, string tooltip)
		{
			bool accepting = ImGuiComponents.IconButton(icon);
			Tooltip(tooltip);
			return accepting;
		}
		public static void Tooltip(string text)
		{
			if (ImGui.IsItemHovered())
			{
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
				ImGui.TextUnformatted(text);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}

		}
	}
}
