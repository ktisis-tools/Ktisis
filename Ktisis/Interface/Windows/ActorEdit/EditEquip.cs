using ImGuiNET;

using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Windows.ActorEdit {
	public class EditEquip {
		// Properties

		public static bool Visible = false;

		public unsafe static Actor* Target => EditActor.Target;

		// UI Code

		public static void Show() => Visible = true;

		public static void Draw() {
			ImGui.EndTabItem();
		}
	}
}