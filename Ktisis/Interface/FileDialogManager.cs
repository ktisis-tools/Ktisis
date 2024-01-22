using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dalamud.Plugin.Services;
using Dalamud.Utility;

using GLib.Popups.ImFileDialog;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Editor.Characters;
using Ktisis.Editor.Posing;

namespace Ktisis.Interface;

public delegate void CharaFileOpenedHandler(string path, CharaFile charaFile);
public delegate void PoseFileOpenedHandler(string path, PoseFile poseFile);

[Singleton]
public class FileDialogManager {
	private readonly ConfigManager _cfg;
	private readonly GuiManager _gui;
	private readonly IFramework _framework;

	private Configuration Config => this._cfg.Config;
	
	public FileDialogManager(
		ConfigManager cfg,
		GuiManager gui,
		IFramework framework
	) {
		this._cfg = cfg;
		this._gui = gui;
		this._framework = framework;
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
	
	// Chara file handling

	public FileDialog OpenCharaFile(
		CharaFileOpenedHandler handler
	) {
		return this.OpenDialog(
			this._gui.AddPopupSingleton(new FileDialog(
				"Open Chara File",
				OnConfirm,
				new FileDialogOptions {
					Flags = FileDialogFlags.OpenMode,
					Filters = "Character Files{.chara}",
					Extension = ".chara"
				}
			))
		);

		void OnConfirm(FileDialog sender, IEnumerable<string> paths) {
			var path = paths.FirstOrDefault();
			if (path.IsNullOrEmpty()) return;
			this.SaveDialogState(sender);

			var content = File.ReadAllText(path);
			var charaFile = new JsonFileSerializer().Deserialize<CharaFile>(content);
			if (charaFile == null) return;
			handler.Invoke(path, charaFile);
		}
	}

	public FileDialog ExportCharaFile(
		EntityCharaConverter chara
	) {
		return this.OpenDialog(
			this._gui.AddPopupSingleton(new FileDialog(
				"Export Chara File",
				OnConfirm,
				new FileDialogOptions {
					Filters = "Character Files{.chara}",
					Extension = ".chara"
				}
			))
		);
		
		void OnConfirm(FileDialog sender, IEnumerable<string> paths) {
			var path = paths.FirstOrDefault();
			if (path.IsNullOrEmpty()) return;
			this.SaveDialogState(sender);

			this._framework.RunOnFrameworkThread(chara.Save).ContinueWith(task => {
				if (task.Exception != null) {
					Ktisis.Log.Error(task.Exception.ToString());
					return;
				}

				var content = new JsonFileSerializer().Serialize(task.Result);
				File.WriteAllText(path, content);
			});
		}
	}
	
	// Pose file handling

	public FileDialog OpenPoseFile(
		PoseFileOpenedHandler handler
	) {
		return this.OpenDialog(
			this._gui.AddPopupSingleton(new FileDialog(
				"Open Pose File",
				OnConfirm,
				new FileDialogOptions {
					Flags = FileDialogFlags.OpenMode,
					Filters = "Pose Files{.pose}",
					Extension = ".pose"
				}
			))
		);
		
		void OnConfirm(FileDialog sender, IEnumerable<string> paths) {
			var path = paths.FirstOrDefault();
			if (path.IsNullOrEmpty()) return;
			this.SaveDialogState(sender);

			var content = File.ReadAllText(path);
			var poseFile = new JsonFileSerializer().Deserialize<PoseFile>(content);
			if (poseFile == null) return;
			handler.Invoke(path, poseFile);
		}
	}

	public FileDialog ExportPoseFile(
		EntityPoseConverter pose
	) {
		return this.OpenDialog(
			this._gui.AddPopupSingleton(new FileDialog(
				"Export Pose File",
				OnConfirm,
				new FileDialogOptions {
					Filters = "Pose Files{.pose}",
					Extension = ".pose"
				}
			))
		);
		
		void OnConfirm(FileDialog sender, IEnumerable<string> paths) {
			var path = paths.FirstOrDefault();
			if (path.IsNullOrEmpty()) return;
			this.SaveDialogState(sender);

			this._framework.RunOnFrameworkThread(pose.Save).ContinueWith(task => {
				if (task.Exception != null) {
					Ktisis.Log.Error(task.Exception.ToString());
					return;
				}

				var content = new JsonFileSerializer().Serialize(task.Result);
				File.WriteAllText(path, content);
			});
		}
	}
}
