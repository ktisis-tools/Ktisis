using System.Numerics;

using ImGuiNET;

using Dalamud.Interface;

using Ktisis.Overlay;

namespace Ktisis.Interface {
	public class KtisisGui {
		public static SkeletonEditor SkeletonEditor { get; set; } = new(); // TODO Code refactor

		public static bool IsInGpose() => Dalamud.PluginInterface.UiBuilder.GposeActive;

		public static void Draw() {
			// MOVE ALL OF THIS
			ImGuiHelpers.ForceNextWindowMainViewport();
			ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

			ImGui.Begin("Ktisis Overlay", ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs);
			ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);

			var draw = ImGui.GetWindowDrawList(); // ESPECIALLY THIS

			Workspace.Draw();
			ConfigGui.Draw();
			CustomizeGui.Draw();

			SkeletonEditor.Draw(draw);

			ImGui.End();
			ImGui.PopStyleVar();
		}

	}
}
