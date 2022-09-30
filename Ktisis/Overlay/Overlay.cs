using System.Numerics;

using ImGuiNET;

namespace Ktisis.Overlay {
	public class Overlay {
		public static void Begin() {
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			ImGui.Begin("Ktisis Overlay", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs);
			ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);
		}

		public static void End() {
			ImGui.End();
			ImGui.PopStyleVar();
		}
	}
}
