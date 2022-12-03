using System;
using System.Numerics;

using ImGuiNET;

using Ktisis.Helpers;

namespace Ktisis.Interface.Windows {
	internal static class Information {
		public static bool Visible = false;

		public static void Show() => Visible = true;
		public static void Toggle() => Visible = !Visible;

		private static Vector2 ButtonSize = new Vector2(0, 25);
		private static Vector4 DiscordColor = new Vector4(86, 98, 246, 255) / 255;
		private static Vector4 KofiColor = new Vector4(255, 91, 94, 255) / 255;

		public static void Draw() {
			if (!Visible) return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin($"Ktisis Info ({Ktisis.Version})", ref Visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.BeginGroup();

				ImGui.Text("Thanks for installing Ktisis!");

				ImGui.Spacing();
				ImGui.Text("The plugin is still in early alpha, so you may experience some issues or bugs.");
				ImGui.Text("As such, it's recommended that you save your progress regularly.");
				ImGui.Text("Please feel free to open threads for issues or suggestions in the Discord!");

				ImGui.Spacing();
				ImGui.Text("You may want to take some time to familiarise yourself with the settings.");
				ImGui.Text("Once you're ready, get started by typing the '/ktisis' command and entering GPose.");

				ImGui.EndGroup();
				ImGui.SameLine(ImGui.GetItemRectSize().X + 50);
				ImGui.BeginGroup();

				ImGui.PushStyleColor(ImGuiCol.Button, DiscordColor);
				if (ImGui.Button("Join us on Discord", ButtonSize))
					Common.OpenBrowser("https://discord.gg/ktisis");
				ImGui.PopStyleColor();

				ImGui.PushStyleColor(ImGuiCol.Button, KofiColor);
				if (ImGui.Button("Support on Ko-fi", ButtonSize))
					Common.OpenBrowser("https://ko-fi.com/chirpcodes");
				ImGui.PopStyleColor();

				if (ImGui.Button("GitHub", ButtonSize))
					Common.OpenBrowser("https://github.com/ktisis-tools/Ktisis/");

				ImGui.Spacing();

				if (ImGui.Button("Open Settings", ButtonSize))
					ConfigGui.Show();

				if (ImGui.Button("Start Posing", ButtonSize))
					Workspace.Workspace.Show();

				ImGui.EndGroup();

				ButtonSize.X = Math.Max(ImGui.GetItemRectSize().X, ButtonSize.X);
			}

			ImGui.PopStyleVar();
			ImGui.End();
		}
	}
}
