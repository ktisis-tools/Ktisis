using Ktisis.Overlay;

namespace Ktisis.Interface {
	public class KtisisGui {
		public static SkeletonEditor SkeletonEditor { get; set; } = new(); // TODO Code refactor

		public static bool IsInGpose() => Dalamud.PluginInterface.UiBuilder.GposeActive;

		public static void Draw() {
			Workspace.Draw();
			ConfigGui.Draw();
			CustomizeGui.Draw();

			SkeletonEditor.Draw();
		}
	}
}
