using System;
using System.IO;
using System.Linq;
using Dalamud.Utility;

using GLib.Popups.ImFileDialog;
using GLib.Popups.ImFileDialog.Data;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Services.Meta;

using DalamudFileManager = Dalamud.Interface.ImGuiFileDialog.FileDialogManager;

namespace Ktisis.Interface;

[Singleton]
public class FileDialogManager {
	private readonly ConfigManager _cfg;
	private readonly ImageDataProvider _img;

	private readonly JsonFileSerializer _serializer = new();
	private readonly DalamudFileManager _fileManager = new();

	public event Action<FileDialog>? OnOpenDialog;
	
	public FileDialogManager(
		ConfigManager cfg,
		ImageDataProvider img
	) {
		this._cfg = cfg;
		this._img = img;
	}
	
	// Initialization

	public void Initialize() => this._img.Initialize();
	public void Draw() => this._fileManager.Draw();
	
	// Dialog state

	private T OpenDialog<T>(T dialog) where T : FileDialog {
		if (this._cfg.File.File.LastOpenedPaths.TryGetValue(dialog.Title, out var path))
			dialog.Open(path);
		else
			dialog.Open();
		this.OnOpenDialog?.Invoke(dialog);
		return dialog;
	}
	
	private void SaveDialogState(FileDialog dialog) {
		if (dialog.ActiveDirectory == null) return;
		this._cfg.File.File.LastOpenedPaths[dialog.Title] = dialog.ActiveDirectory;
	}
	
	// File handling

	public void OpenFile(
		string name,
		Action<string> handler,
		FileDialogOptions? options = null
	) {
		//
		options ??= new FileDialogOptions();
		this.PopulateOptions(options);

		Ktisis.Log.Info("Opening file dialog...");
		
		this._fileManager.OpenFileDialog(
			name,
			options.Filters,
			(isOk, paths) => {
				if (!isOk) return;
				
				//this.SaveDialogState(sender);
				var path = paths.FirstOrDefault();
				if (path.IsNullOrEmpty()) return;
				handler.Invoke(path);
			},
			options.MaxOpenCount,
			null,
			true
		);
	}

	public void OpenFile<T>(
		string name,
		Action<string, T> handler,
		FileDialogOptions? options = null
	) where T : JsonFile {
		this.OpenFile(name, path => {
			var content = File.ReadAllText(path);
			if (Path.GetExtension(path).Equals(".cmp")) content = LegacyPoseHelpers.ConvertLegacyPose(content);
			var file = this._serializer.Deserialize<T>(content);
			if (file != null) handler.Invoke(path, file);
		}, options);
	}

	public void SaveFile(
		string name,
		string content,
		FileDialogOptions? options = null
	) {
		options ??= new ();
		this.PopulateOptions(options);
		
		var defaultFilename = options.DefaultFileName;
		if (options.Extension is not null && !defaultFilename.EndsWith(options.Extension)) 
			defaultFilename += options.Extension;
		
		this._fileManager.SaveFileDialog(
			name,
			options.Filters,
			defaultFilename,
			options.Extension ?? "",
			(isOk, path) =>
			{
				if (!isOk || path.IsNullOrEmpty()) return;
				
				File.WriteAllText(path, content);
			}, 
			null,
			isModal: true
		);
	}

	public void SaveFile<T>(
		string name,
		T file,
		FileDialogOptions? options = null
	) where T : JsonFile {
		var content = this._serializer.Serialize(file);
		this.SaveFile(name, content, options);
	}
		
	// Image handling

	private readonly FileDialogOptions ImageOptions = new() {
		Flags = FileDialogFlags.OpenMode,
		Filters = "Images{.png,.jpg,.jpeg}"
	};

	public void OpenImage(
		string name,
		Action<string> handler
	) {
		var dialog = new FileDialog(name, (sender, paths) => {
			foreach (var path in paths)
				handler.Invoke(path);
		}, this.ImageOptions);
		
		this._img.BindMetadata(dialog);
		this.OpenDialog(dialog);
	}
	
	// Options

	private FileDialogLocation? AutoSaveLoc;

	private void PopulateOptions(FileDialogOptions options) {
		var savePath = this._cfg.File.AutoSave.FilePath;
		if (this.AutoSaveLoc == null)
			this.AutoSaveLoc = new FileDialogLocation("AutoSave", savePath);
		else if (this.AutoSaveLoc.FullPath != savePath)
			this.AutoSaveLoc.FullPath = savePath;
		
		if (!options.Locations.Contains(this.AutoSaveLoc)) {
			options.Locations.Add(this.AutoSaveLoc);
			Ktisis.Log.Debug($"Added autosave: {savePath}");
		}
	}
}
