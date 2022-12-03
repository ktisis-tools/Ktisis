using System.Numerics;

using Dalamud.Interface;

using ImGuiNET;

using Ktisis.Interface.Components;
using Ktisis.Interface.Windows.Workspace;
using Ktisis.Interop.Hooks;
using Ktisis.Overlay;
using Ktisis.Structs.Actor;
using Ktisis.Util;

namespace Ktisis.Interface.Windows.Toolbar {
	public static class BonesWindow {
		private static bool Visible = false;

		// Toggle visibility
		public static void Toggle() => Visible = !Visible;

		// Draw window
		public unsafe static void Draw() {
			if (!Visible || !Ktisis.IsInGPose)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(new Vector2(ImGui.GetFontSize() * 16, 1), new Vector2(50000, 50000));
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Bones", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)) {

				var cfg = Ktisis.Configuration;
				var target = Ktisis.GPoseTarget;
				var actor = (Actor*)target!.Address;

				// Bone categories
				if (!Categories.DrawToggleList(cfg)) {
					ImGui.Text("No bone found.");
					ImGui.Text("Show Skeleton (");
					ImGui.SameLine();
					GuiHelpers.Icon(FontAwesomeIcon.EyeSlash);
					ImGui.SameLine();
					ImGui.Text(") to fill this.");
				}
				
				// Bone tree
				BoneTree.Draw(actor);
			}

			ImGui.PopStyleVar();
			ImGui.End();
		}
	}

}