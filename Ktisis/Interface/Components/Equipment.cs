using ImGuiNET;

using Ktisis.Util;
using Dalamud.Interface;

namespace Ktisis.Interface.Components {

	public class Equipment {
		public static void CreateGlamourQuestionPopup()
		{
			GuiHelpers.PopupConfirm(
				"##popup_glamour_plate_use##1",
				() => {
					ImGui.Text("Every time a Glamour Plate windows is closed,\nthe Glamour Plates memory is updated.\n\nTo populate them right now, open the General Skill \"Glamour Plate\",\nand close it.\nThis skill requires being in a sanctuary.\n\nIt can be summoned by typing this chat command:");
					ImGui.Text("/ac \"Glamour Plate\"");
					ImGui.SameLine();
					if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Clipboard, "Copy the command /ac \"Glamour Plate\" to the clipboard."))
						ImGui.SetClipboardText("/ac \"Glamour Plate\"");
				},
				null,
				true);
		}
		public static void OpenGlamourQuestionPopup()
		{
			ImGui.OpenPopup("##popup_glamour_plate_use##1");
		}
	}
}