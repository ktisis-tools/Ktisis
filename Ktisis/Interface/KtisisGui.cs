using Dalamud.Logging;

using Ktisis.Overlay;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.ActorEdit;

namespace Ktisis.Interface {
	public class KtisisGui {
		public static SkeletonEditor SkeletonEditor { get; set; } = new(); // TODO Code refactor

		public static bool IsInGpose() => Dalamud.PluginInterface.UiBuilder.GposeActive;

		public static void Draw() {
			Workspace.Draw();
			ConfigGui.Draw();
			EditActor.Draw();

			SkeletonEditor.Draw();
		}
	}
}