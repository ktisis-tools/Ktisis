using System;
using System.Collections.Generic;
using System.Reflection;

using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;

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

		public void OpenFolderDialog(string title, Action<bool, string> callback) {
			setupDialog("");
			baseInstance.OpenFolderDialog(title, (selected, path) => {
				isClosing = true;
				callback(selected, path);
			});
		}

		public void OpenFolderDialog(string title, Action<bool, string> callback, string? startPath, bool isModal = false) {
			setupDialog("");
			baseInstance.OpenFolderDialog(title, (selected, path) => {
				isClosing = true;
				callback(selected, path);
			}, startPath, isModal);
		}

		public void SaveFolderDialog(string title, string defaultFolderName, Action<bool, string> callback) {
			setupDialog("");
			baseInstance.SaveFolderDialog(title, defaultFolderName, (selected, path) => {
				isClosing = true;
				callback(selected, path);
			});
		}

		public void SaveFolderDialog(string title, string defaultFolderName, Action<bool, string> callback, string? startPath, bool isModal = false) {
			setupDialog("");
			baseInstance.SaveFolderDialog(title, defaultFolderName, (selected, path) => {
				isClosing = true;
				callback(selected, path);
			}, startPath, isModal);
		}

		public void OpenFileDialog(string title, string filters, Action<bool, string> callback) {
			setupDialog(filters);
			baseInstance.OpenFileDialog(title, filters, (selected, path) => {
				isClosing = true;
				callback(selected, path);
			});
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
			baseInstance.OpenFileDialog(title, filters, (selected, path) => {
				isClosing = true;
				callback(selected, path);
			}, selectionCountMax, startPath, isModal);
		}

		public void SaveFileDialog(
			string title,
			string filters,
			string defaultFileName,
			string defaultExtension,
			Action<bool, string> callback
		) {
			setupDialog(filters);
			baseInstance.SaveFileDialog(title, filters, defaultFileName, defaultExtension, (selected, path) => {
				isClosing = true;
				callback(selected, path);
			});
		}

		public void SaveFileDialog(
			string title,
			string filters,
			string defaultFileName,
			string defaultExtension,
			Action<bool, string> callback,
			string? startPath,
			bool isModal = false
		) {
			setupDialog(filters);
			baseInstance.SaveFileDialog(
				title,
				filters,
				defaultFileName,
				defaultExtension,
				(selected, path) => {
					isClosing = true;
					callback(selected, path);
				},
				startPath,
				isModal
			);
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
			if (savedPath == ".") {
				if (Ktisis.Configuration.SavedDirPaths.TryGetValue(filters, out string? path)) {
					savedPath = path;
				}
			}
		}
	}
}
