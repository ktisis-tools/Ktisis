using System;
using System.IO;
using System.Linq;

using Dalamud.Utility;

using GLib.Popups.ImFileDialog;

using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Data.Files;
using Ktisis.Data.Json;
using Ktisis.Services.Meta;

namespace Ktisis.Interface;

[Singleton]
public class FileDialogManager {
	private readonly ConfigManager _cfg;
	private readonly ImageDataProvider _img;

	private readonly JsonFileSerializer _serializer = new();

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

	public FileDialog OpenFile(
		string name,
		Action<string> handler,
		FileDialogOptions? options = null
	) {
		options ??= new FileDialogOptions();

		var dialog = new FileDialog(name, (sender, paths) => {
			this.SaveDialogState(sender);
			var path = paths.FirstOrDefault();
			if (path.IsNullOrEmpty()) return;
			handler.Invoke(path);
		}, options with { Flags = FileDialogFlags.OpenMode });
		
		return this.OpenDialog(dialog);
	}

	public FileDialog OpenFile<T>(
		string name,
		Action<string, T> handler,
		FileDialogOptions? options = null
	) where T : JsonFile {
		return this.OpenFile(name, path => {
			var content = File.ReadAllText(path);
			var file = this._serializer.Deserialize<T>(content);
			if (file != null) handler.Invoke(path, file);
		}, options);
	}

	public FileDialog SaveFile(
		string name,
		string content,
		FileDialogOptions? options = null
	) {
		options ??= new FileDialogOptions();

		var dialog = new FileDialog(name, (sender, paths) => {
			this.SaveDialogState(sender);
			var path = paths.FirstOrDefault();
			if (path.IsNullOrEmpty()) return;
			
			File.WriteAllText(path, content);
		}, options);

		return this.OpenDialog(dialog);
	}

	public FileDialog SaveFile<T>(
		string name,
		T file,
		FileDialogOptions? options = null
	) where T : JsonFile {
		var content = this._serializer.Serialize(file);
		return this.SaveFile(name, content, options);
	}
		
	// Image handling

	private readonly FileDialogOptions ImageOptions = new() {
		Flags = FileDialogFlags.OpenMode,
		Filters = "Images{.png,.jpg,.jpeg}"
	};

	public FileDialog OpenImage(
		string name,
		Action<string> handler
	) {
		var dialog = new FileDialog(name, (sender, paths) => {
			foreach (var path in paths)
				handler.Invoke(path);
		}, this.ImageOptions);
		
		this._img.BindMetadata(dialog);
		return this.OpenDialog(dialog);
	}
}
