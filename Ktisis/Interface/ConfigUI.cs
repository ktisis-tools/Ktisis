using System.Numerics;

using ImGuiNET;

namespace Ktisis.Interface {
	internal class ConfigUI {
		private Ktisis Plugin;

		public bool Visible = false;

		// Constructor

		public ConfigUI(Ktisis plugin) {
			Plugin = plugin;
		}

		// Toggle visibility

		public void Show() {
			Visible = true;
		}

		public void Hide() {
			Visible = false;
		}

		// Draw

		public void Draw() {
			if (!Visible)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);
			ImGui.SetNextWindowSizeConstraints(size, size);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Ktisis Settings", ref Visible, ImGuiWindowFlags.NoResize)) {
				if (ImGui.BeginTabBar("Settings")) {
					if (ImGui.BeginTabItem("Overlay"))
						DrawOverlayTab();
					if (ImGui.BeginTabItem("Gizmo"))
						DrawGizmoTab();
					ImGui.EndTabBar();
				}
			}

			ImGui.PopStyleVar(1);
			ImGui.End();
		}

		// Overlay

		public void DrawOverlayTab() {
			ImGui.Text("Overlay");
			ImGui.EndTabItem();
		}

		// Gizmo

		public void DrawGizmoTab() {
			ImGui.Text("Gizmo");
			ImGui.EndTabItem();
		}
	}
}
