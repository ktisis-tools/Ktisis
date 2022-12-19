using Ktisis.Interface.Windows;
using Ktisis.Interface.Overlay;

namespace Ktisis.Interface {
    public static class KtisisGui {
		//public static FileDialogManager FileDialogManager = new FileDialogManager();

		static KtisisGui() {
			/*FileDialogManager.CustomSideBarItems.Add((
				"Anamnesis",
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Anamnesis",
				FontAwesomeIcon.None,
				0
			));*/
		}

		public static void Draw() {
			//FileDialogManager.Draw();

			// Overlay
			OverlayWindow.Draw();

			// GUI
			Workspace.Draw();
			//ConfigGui.Draw();
			//Information.Draw();
			//EditActor.Draw();
			//References.Draw();
			//ToolbarWindow.Draw();
		}
	}
}
