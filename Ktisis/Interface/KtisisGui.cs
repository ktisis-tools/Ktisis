using Ktisis.Overlay;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Interface.Windows.Workspace;

namespace Ktisis.Interface {
	public static class KtisisGui {
		public static void Draw() {
			// Overlay
			OverlayWindow.Draw();

			// GUI
			Workspace.Draw();
			ConfigGui.Draw();
			EditActor.Draw();
		}
	}
}