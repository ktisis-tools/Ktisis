using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Ktisis.Data.Config;
using Ktisis.Data.Config.Sections;
using Ktisis.Data.Json;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Types;
using Ktisis.Services.Data;

namespace Ktisis.Editor.Posing.AutoSave;

public class PoseAutoSave : IDisposable {
	private readonly IEditorContext _ctx;
	private readonly IFramework _framework;
	private readonly FormatService _format;
	
	private IPosingManager Posing => this._ctx.Posing;
	private ISceneManager Scene => this._ctx.Scene;

	private readonly Timer _timer = new();
	private readonly Queue<string> _prefixes = new();
	
	private AutoSaveConfig _cfg = null!;
	
	public PoseAutoSave(
		IEditorContext ctx,
		IFramework framework,
		FormatService format
	) {
		this._ctx = ctx;
		this._framework = framework;
		this._format = format;
	}

	public void Initialize(Configuration cfg) {
		this._timer.Elapsed += this.OnElapsed;
		this.Configure(cfg);
	}

	public void Configure(Configuration cfg) {
		this._cfg = cfg.AutoSave;
		this._timer.Interval = TimeSpan.FromSeconds(this._cfg.Interval).TotalMilliseconds;
		if (this._timer.Enabled != this._cfg.Enabled)
			this._timer.Enabled = this._cfg.Enabled;
	}

	private async void OnElapsed(object? sender, ElapsedEventArgs e) {
		if (!this.Posing.IsValid) {
			this._timer.Stop();
			return;
		}

		if (!this._cfg.Enabled || !this.Posing.IsEnabled)
			return;
		
		try {
			await this._framework.RunOnFrameworkThread(this.Save);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to save poses:\n{err}");
		}
	}

	public void Save() {
		var prefix = this._format.Replace(this._cfg.FolderFormat);
		var folder = Path.Combine(this._cfg.FilePath, prefix);
		this._prefixes.Enqueue(prefix);

		if (!Directory.Exists(folder))
			Directory.CreateDirectory(folder);
		
		var entities = this.Scene.Children
			.Where(entity => entity is CharaEntity)
			.Cast<CharaEntity>()
			.ToList();

		if (entities.Count == 0) {
			Ktisis.Log.Warning("No valid entities, skipping auto save.");
			return;
		}
		
		Ktisis.Log.Info($"Auto saving poses for {entities.Count} character(s)");
		
		foreach (var chara in entities) {
			if (chara.Pose == null) continue;

			var dupeCt = 1;
			var name = this._format.StripInvalidChars(chara.Name);
			var path = Path.Combine(folder, $"{name}.pose");
			while (Path.Exists(path))
				path = Path.Combine(folder, $"{name} ({++dupeCt}).pose");

			var serializer = new JsonFileSerializer();
			var file = new EntityPoseConverter(chara.Pose).SaveFile();
			File.WriteAllText(path, serializer.Serialize(file));
		}
		
		Ktisis.Log.Verbose($"Prefix count: {this._prefixes.Count} max: {this._cfg.Count}");
		while (this._prefixes.Count > this._cfg.Count)
			this.DeleteOldest();
	}

	private void DeleteOldest() {
		var oldest = this._prefixes.Dequeue();
		var folder = Path.Combine(this._cfg.FilePath, oldest);
		if (Directory.Exists(folder)) {
			Ktisis.Log.Verbose($"Deleting {folder}");
			Directory.Delete(folder, true);
		}
		DeleteEmptyDirs(this._cfg.FilePath);
	}

	private static void DeleteEmptyDirs(string dir) {
		if (dir.IsNullOrEmpty())
			throw new ArgumentException("Starting directory is a null reference or empty string", nameof(dir));

		try {
			foreach (var subDir in Directory.EnumerateDirectories(dir))
				DeleteEmptyDirs(subDir);

			var entries = Directory.EnumerateFileSystemEntries(dir);
			if (entries.Any()) return;

			try {
				Directory.Delete(dir);
			} catch (DirectoryNotFoundException) { }
		} catch (UnauthorizedAccessException err) {
			Ktisis.Log.Error(err.ToString());
		}
	}

	private void Clear() {
		try {
			while (this._cfg.ClearOnExit && this._prefixes.Count > 0)
				this.DeleteOldest();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to clear auto saves:\n{err}");
		}
	}

	public void Dispose() {
		this._timer.Elapsed -= this.OnElapsed;
		this._timer.Stop();
		this._timer.Dispose();
		this.Clear();
		GC.SuppressFinalize(this);
	}
}
