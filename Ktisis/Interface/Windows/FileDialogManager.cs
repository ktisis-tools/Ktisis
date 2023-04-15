using System;
using System.Reflection;
using System.Collections.Generic;

using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;

using Ktisis.Helpers;

using BaseDialogManager = Dalamud.Interface.ImGuiFileDialog.FileDialogManager;

namespace Ktisis.Interface.Windows {
	public class FileDialogManager {
	
		public List<(string Name, string Path, FontAwesomeIcon Icon, int Positon)> CustomSideBarItems => baseInstance.CustomSideBarItems;

		private readonly BaseDialogManager baseInstance = new();

		private static readonly FieldInfo baseDialogField = typeof(BaseDialogManager).GetField("dialog", BindingFlags.Instance | BindingFlags.NonPublic)!;
		private static readonly FieldInfo baseSavedPathField = typeof(BaseDialogManager).GetField("savedPath", BindingFlags.Instance | BindingFlags.NonPublic)!;
		private bool isClosing = false;
		private string currentFilter = "";

		private FileDialog? dialog => (FileDialog?) baseDialogField.GetValue(baseInstance);

		private string savedPath {
			get => (string) baseSavedPathField.GetValue(baseInstance)!;
			set => baseSavedPathField.SetValue(baseInstance, value);
		}


		public void OpenFileDialog(string title, string filters, Action<bool, string> callback) {
			setupDialog(filters);
			try {
				baseInstance.OpenFileDialog(title, filters, (selected, path) => {
					isClosing = true;
					callback(selected, path);
				});
			} catch {
				savedPath = ".";
			}
		}

		public void OpenFileDialog(
			string title,
			string filters,
			Action<bool, List<string>> callback,
			int selectionCountMax,
			string? startPath = null,
			bool isModal = false
		) {
			setupDialog(filters);
			try {
				baseInstance.OpenFileDialog(title, filters, (selected, path) => {
					isClosing = true;
					callback(selected, path);
				}, selectionCountMax, startPath, isModal);
			} catch {
				savedPath = ".";
			}
		}

		public void SaveFileDialog(
			string title,
			string filters,
			string defaultFileName,
			string defaultExtension,
			Action<bool, string> callback
		) {
			setupDialog(filters);
			try {
				baseInstance.SaveFileDialog(title, filters, defaultFileName, defaultExtension, (selected, path) => {
					isClosing = true;
					callback(selected, path);
				});
			} catch {
				savedPath = ".";
			}
		}

		public void Draw() {
			baseInstance.Draw();
			if (isClosing) {
				isClosing = false;
				if (currentFilter != "") {
					Ktisis.Configuration.SavedDirPaths[currentFilter] = savedPath;
					savedPath = ".";
				}
			}
		}

		private void setupDialog(string filters) {
			currentFilter = filters;
			if (savedPath != ".") return;
			
			if (Ktisis.Configuration.SavedDirPaths.TryGetValue(filters, out string? path) && Common.IsPathValid(path))
				savedPath = path;
		}
	}
}
