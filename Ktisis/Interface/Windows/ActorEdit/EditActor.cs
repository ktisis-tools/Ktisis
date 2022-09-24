using System.Numerics;

using ImGuiNET;

using Ktisis.Structs.Actor;

namespace Ktisis.Interface.Windows.ActorEdit {
	internal class EditActor {
		// Properties

		public static bool Visible = false;

		public unsafe static Actor* Target
			=> Ktisis.GPoseTarget != null ? (Actor*)Ktisis.GPoseTarget.Address : null;

		// Toggle visibility

		public static void Show() => Visible = true;

		// Display

		public unsafe static void Draw() {
			if (!Visible)
				return;

			if (Target == null)
				return;

			var size = new Vector2(400, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			// Create window
			var title = Ktisis.Configuration.DisplayCharName ? $"{Target->Name}" : "Appearance";
			if (ImGui.Begin(title, ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize)) {
				ImGui.BeginGroup();
				ImGui.AlignTextToFramePadding();

				if (ImGui.BeginTabBar("Settings")) {
					if (ImGui.BeginTabItem("Customize"))
						EditCustomize.Draw();
					if (ImGui.BeginTabItem("Equipment"))
						ImGui.EndTabItem();
					if (ImGui.BeginTabItem("Advanced"))
						ImGui.EndTabItem();

					ImGui.EndTabBar();
				}

				ImGui.PopStyleVar(1);
				ImGui.End();
			}
		}
	}
}