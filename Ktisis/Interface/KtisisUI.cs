using System.Numerics;

using ImGuiNET;

namespace Ktisis.Interface {
	internal class KtisisUI {
		public Ktisis Plugin;

		public bool Visible = false;

		// Constructor

		public KtisisUI(Ktisis plogon) {
			Plugin = plogon;
		}

		// Toggle visibility

		public void Show() {
			Visible = true;
		}

		public void Hide() {
			Visible = false;
		}

		// Draw window

		public void Draw() {
			if (!Visible)
				return;

			var gposeEnabled = Plugin.IsInGpose();

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);
			ImGui.SetNextWindowSizeConstraints(size, size);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Ktisis", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize)) {
				var cfg = Plugin.Configuration;

				ImGui.BeginGroup();
				ImGui.AlignTextToFramePadding();

				if (ImGui.Button("Settings")) {
					// TODO
				}

				ImGui.Separator();

				var showSkeleton = cfg.ShowSkeleton;
				if (ImGui.Checkbox("Show Skeleton", ref showSkeleton)) {
					cfg.ShowSkeleton = showSkeleton;
					cfg.Save(Plugin);
				}

				var _ = false;
				if (ImGui.Checkbox("Enable Posing", ref _)) {
					// TODO
				}

				ImGui.Separator();
			}

			ImGui.PopStyleVar(1);

			ImGui.End();
		}
	}
}
