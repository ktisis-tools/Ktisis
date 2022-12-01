using System;
using System.Collections.Generic;

using ImGuiNET;

using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Logging;

namespace Ktisis.Interface.Windows;

// Copying this because we need to change stuff and the original class makes everything private :(
// https://github.com/goatcorp/Dalamud/blob/master/Dalamud/Interface/ImGuiFileDialog/FileDialogManager.cs

public class FileDialogManager {
#pragma warning disable SA1401
	public readonly List<(string Name, string Path, FontAwesomeIcon Icon, int Position)> CustomSideBarItems = new();

	public ImGuiWindowFlags AddedWindowFlags = ImGuiWindowFlags.None;
#pragma warning restore SA1401

	private FileDialog? dialog;
	private Action<bool, string>? callback;
	private Action<bool, List<string>>? multiCallback;
	private string savedPath = ".";

	private string _filter = "";

	public void OpenFolderDialog(string title, Action<bool, string> callback) {
		SetDialog("OpenFolderDialog", title, string.Empty, savedPath, ".", string.Empty, 1, false, ImGuiFileDialogFlags.SelectOnly, callback);
	}

	public void OpenFolderDialog(string title, Action<bool, string> callback, string? startPath, bool isModal = false) {
		SetDialog("OpenFolderDialog", title, string.Empty, startPath ?? savedPath, ".", string.Empty, 1, isModal, ImGuiFileDialogFlags.SelectOnly, callback);
	}

	public void SaveFolderDialog(string title, string defaultFolderName, Action<bool, string> callback) {
		SetDialog("SaveFolderDialog", title, string.Empty, savedPath, defaultFolderName, string.Empty, 1, false, ImGuiFileDialogFlags.None, callback);
	}

	public void SaveFolderDialog(string title, string defaultFolderName, Action<bool, string> callback, string? startPath, bool isModal = false) {
		SetDialog("SaveFolderDialog", title, string.Empty, startPath ?? savedPath, defaultFolderName, string.Empty, 1, isModal, ImGuiFileDialogFlags.None, callback);
	}

	public void OpenFileDialog(string title, string filters, Action<bool, string> callback) {
		SetDialog("OpenFileDialog", title, filters, savedPath, ".", string.Empty, 1, false, ImGuiFileDialogFlags.SelectOnly, callback);
	}

	public void OpenFileDialog(
		string title,
		string filters,
		Action<bool, List<string>> callback,
		int selectionCountMax,
		string? startPath = null,
		bool isModal = false) {
		SetDialog("OpenFileDialog", title, filters, startPath ?? savedPath, ".", string.Empty, selectionCountMax, isModal, ImGuiFileDialogFlags.SelectOnly, callback);
	}

	public void SaveFileDialog(
		string title,
		string filters,
		string defaultFileName,
		string defaultExtension,
		Action<bool, string> callback) {
		SetDialog("SaveFileDialog", title, filters, savedPath, defaultFileName, defaultExtension, 1, false, ImGuiFileDialogFlags.None, callback);
	}

	public void SaveFileDialog(
		string title,
		string filters,
		string defaultFileName,
		string defaultExtension,
		Action<bool, string> callback,
		string? startPath,
		bool isModal = false) {
		SetDialog("SaveFileDialog", title, filters, startPath ?? savedPath, defaultFileName, defaultExtension, 1, isModal, ImGuiFileDialogFlags.None, callback);
	}

	public void Draw() {
		if (dialog == null) return;
		if (dialog.Draw()) {
			var isOk = dialog.GetIsOk();
			var results = dialog.GetResults();
			callback?.Invoke(isOk, results.Count > 0 ? results[0] : string.Empty);
			multiCallback?.Invoke(isOk, results);
			savedPath = dialog.GetCurrentPath();

			Reset();
		}
	}

	public void Reset() {
		if (dialog != null && _filter != "") {
			Ktisis.Configuration.SavedDirPaths[_filter] = savedPath;
			savedPath = ".";
		}

		dialog?.Hide();
		dialog = null;
		callback = null;
		multiCallback = null;
	}

	private void SetDialog(
		string id,
		string title,
		string filters,
		string path,
		string defaultFileName,
		string defaultExtension,
		int selectionCountMax,
		bool isModal,
		ImGuiFileDialogFlags flags,
		Delegate callback
	) {
		Reset();
		if (callback is Action<bool, List<string>> multi) {
			multiCallback = multi;
		} else {
			callback = (Action<bool, string>)callback;
		}

		_filter = filters;
		if (path == ".") {
			if (Ktisis.Configuration.SavedDirPaths.TryGetValue(filters, out var newPath))
				path = newPath;
		}

		dialog = new FileDialog(id, title, filters, path, defaultFileName, defaultExtension, selectionCountMax, isModal, flags);
		dialog.WindowFlags |= AddedWindowFlags;
		foreach (var (name, location, icon, position) in CustomSideBarItems)
			dialog.SetQuickAccess(name, location, icon, position);
		dialog.Show();
	}
}