using System;

using Dalamud.Interface;

using Ktisis.Overlay;
using Ktisis.Interface.Windows;
using Ktisis.Interface.Windows.ActorEdit;
using Ktisis.Interface.Windows.Toolbar;
using Ktisis.Interface.Windows.Workspace;

namespace Ktisis.Interface {
	public static class KtisisGui {
		public static FileDialogManager FileDialogManager = new FileDialogManager();

		static KtisisGui() {
			FileDialogManager.CustomSideBarItems.Add((
				"Anamnesis",
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Anamnesis",
				FontAwesomeIcon.None,
				0
			));

			if (Ktisis.Configuration.EnableAutoSave) {
				FileDialogManager.CustomSideBarItems.Add((
					"AutoSave",
					Ktisis.Configuration.AutoSavePath,
					FontAwesomeIcon.Bookmark,
					-1
				));
			}
		}

		public static void Draw() {
			FileDialogManager.Draw();

			// Overlay
			OverlayWindow.Draw();

			// GUI
			Workspace.Draw();
			ConfigGui.Draw();
			Information.Draw();
			EditActor.Draw();
			References.Draw();
			ToolbarWindow.Draw();
		}
	}
}
