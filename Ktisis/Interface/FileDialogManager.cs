using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dalamud.Utility;

using GLib.Popups.ImFileDialog;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Files;
using Ktisis.Data.Json;

namespace Ktisis.Interface;

public delegate void PoseFileOpenedHandler(string path, PoseFile poseFile);

[Singleton]
public class FileDialogManager {
	private readonly ConfigManager _cfg;
	private readonly GuiManager _gui;

	private Configuration Config => this._cfg.Config;
	
	public FileDialogManager(
		ConfigManager cfg,
		GuiManager gui
	) {
		this._cfg = cfg;
		this._gui = gui;
	}
	
	// Dialog state

	private T OpenDialog<T>(T dialog) where T : FileDialog {
		if (this.Config.File.LastOpenedPaths.TryGetValue(dialog.Title, out var path))
			dialog.Open(path);
		else
			dialog.Open();
		return dialog;
	}
	
	private void SaveDialogState(FileDialog dialog) {
		if (dialog.ActiveDirectory == null) return;
		this.Config.File.LastOpenedPaths[dialog.Title] = dialog.ActiveDirectory;
	}
	
	// File type handlers

	public FileDialog OpenPoseFile(
		PoseFileOpenedHandler handler
	) {
		return this.OpenDialog(this._gui.AddPopupSingleton(new FileDialog(
			"Import Pose",
			OnConfirm,
			new FileDialogOptions {
				Flags = FileDialogFlags.OpenMode,
				Filters = "Pose Files{.pose}",
				Extension = ".pose"
			}
		)));
		
		void OnConfirm(FileDialog sender, IEnumerable<string> paths) {
			var path = paths.FirstOrDefault();
			if (path.IsNullOrEmpty()) return;

			this.SaveDialogState(sender);

			var poseFile = new JsonFileSerializer()
				.Deserialize<PoseFile>(File.ReadAllText(path));
			
			if (poseFile != null)
				handler.Invoke(path, poseFile);
		}
	}
}
