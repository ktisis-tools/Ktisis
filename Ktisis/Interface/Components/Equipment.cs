using System.Numerics;
using ImGuiNET;

using FFXIVClientStructs.Havok;

using Ktisis.Helpers;
using Ktisis.Structs;
using Ktisis.Util;

namespace Ktisis.Interface.Components {

	public class Equipment {
		public static void CreateGlamourQuestionPopup()
		{
			GuiHelpers.PopupConfirm(
				"##popup_glamour_plate_use##1",
				() => ImGui.Text("Every time a Glamour Plate windows is closed, the Glamour Plates memory is updated.\n\nWould you like to open Glamour Plates?"),
				() => Structs.Actor.EquipmentSetSources.GlamourDresser.PopupOfferOpenGlamourPlates_confirmed());
		}
		public static void OpenGlamourQuestionPopup()
		{
			ImGui.OpenPopup("##popup_glamour_plate_use##1");
		}
	}
}