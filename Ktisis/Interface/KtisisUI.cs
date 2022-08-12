using System.Numerics;

using ImGuiNET;

namespace Ktisis.Interface {
	internal class KtisisUI {
		private Ktisis Plugin;

		public bool Visible = false;

		public static Vector4 ColGreen = new Vector4(0, 255, 0, 255);
		public static Vector4 ColRed = new Vector4(255, 0, 0, 255);

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

				ImGui.TextColored(
					gposeEnabled ? ColGreen : ColRed,
					gposeEnabled ? "GPose Enabled" : "GPose Disabled"
				);

				ImGui.BeginGroup();
				ImGui.AlignTextToFramePadding();

				//ImGui.Text(string.Format("In GPose: {0}", gposeEnabled));

				if (ImGui.Button("Settings"))
					Plugin.ConfigInterface.Show();

				ImGui.Separator();

				var showSkeleton = cfg.ShowSkeleton;
				if (ImGui.Checkbox("Toggle Skeleton", ref showSkeleton)) {
					cfg.ShowSkeleton = showSkeleton;
					cfg.Save(Plugin);
				}

				var _ = false;
				if (ImGui.Checkbox("Toggle Posing", ref _)) {
					// TODO
				}

				ImGui.Separator();
			}

			ImGui.PopStyleVar(1);
			ImGui.End();
		}
	}
}
