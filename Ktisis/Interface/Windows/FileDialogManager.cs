using System;
using System.Collections.Generic;
using System.Reflection;

using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;

using BaseDialogManager = Dalamud.Interface.ImGuiFileDialog.FileDialogManager;

namespace Ktisis.Interface.Windows;


public class FileDialogManager {
	
	public List<(string Name, string Path, FontAwesomeIcon Icon, int Positon)> CustomSideBarItems {
		get => baseInstance.CustomSideBarItems;
	}
	
	private BaseDialogManager baseInstance = new BaseDialogManager();

	private static FieldInfo baseDialogField = typeof(BaseDialogManager).GetField("dialog", BindingFlags.Instance | BindingFlags.NonPublic)!;
	private static FieldInfo baseSavedPathField = typeof(BaseDialogManager).GetField("savedPath", BindingFlags.Instance | BindingFlags.NonPublic)!;
	private bool isClosing = false;
	private string currentFilter = "";

	private FileDialog? dialog {
		get => (FileDialog?) baseDialogField.GetValue(this.baseInstance);
	}

	private string savedPath {
		get => (string) baseSavedPathField.GetValue(this.baseInstance)!;
		set => baseSavedPathField.SetValue(this.baseInstance, value);
	}

	public void OpenFolderDialog(string title, Action<bool, string> callback) {
		this.setupDialog("");
		baseInstance.OpenFolderDialog(title, (selected, path) => {
			this.isClosing = true;
			callback(selected, path);
		});
	}

	public void OpenFolderDialog(string title, Action<bool, string> callback, string? startPath, bool isModal = false) {
		this.setupDialog("");
		baseInstance.OpenFolderDialog(title, (selected, path) => {
			this.isClosing = true;
			callback(selected, path);
		}, startPath, isModal);
	}

	public void SaveFolderDialog(string title, string defaultFolderName, Action<bool, string> callback) {
		this.setupDialog("");
		baseInstance.SaveFolderDialog(title, defaultFolderName, (selected, path) => {
			this.isClosing = true;
			callback(selected, path);
		});
	}

	public void SaveFolderDialog(string title, string defaultFolderName, Action<bool, string> callback, string? startPath, bool isModal = false) {
		this.setupDialog("");
		baseInstance.SaveFolderDialog(title, defaultFolderName, (selected, path) => {
			this.isClosing = true;
			callback(selected, path);
		}, startPath, isModal);
	}

	public void OpenFileDialog(string title, string filters, Action<bool, string> callback) {
		this.setupDialog(filters);
		baseInstance.OpenFileDialog(title, filters, (selected, path) => {
			this.isClosing = true;
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
	this.setupDialog(filters);
		baseInstance.OpenFileDialog(title, filters, (selected, path) => {
			this.isClosing = true;
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
		this.setupDialog(filters);
		baseInstance.SaveFileDialog(title, filters, defaultFileName, defaultExtension, (selected, path) => {
			this.isClosing = true;
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
		this.setupDialog(filters);
		this.baseInstance.SaveFileDialog(
			title,
			filters,
			defaultFileName,
			defaultExtension,
			(selected, path) => {
				this.isClosing = true;
				callback(selected, path);
			},
			startPath,
			isModal
		);
	}

	public void Draw() {
		this.baseInstance.Draw();
		if (isClosing) {
			if (this.currentFilter != "")
				Ktisis.Configuration.SavedDirPaths[this.currentFilter] = this.savedPath;
		}
	}

	private void setupDialog(string filters) {
		currentFilter = filters;
		if (this.savedPath == ".") {
			if (Ktisis.Configuration.SavedDirPaths.TryGetValue(filters, out string? path)) {
				this.savedPath = path;
			}
		}
	}
}
