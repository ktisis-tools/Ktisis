using System;
using System.IO;
using System.Linq;
using System.Timers;

using Dalamud.Plugin.Services;

using Ktisis.Data.Config;
using Ktisis.Data.Config.Sections;
using Ktisis.Data.Json;
using Ktisis.Editor.Context.Types;
using Ktisis.Editor.Posing.Data;
using Ktisis.Editor.Posing.Types;
using Ktisis.Scene.Entities.Character;
using Ktisis.Scene.Types;

namespace Ktisis.Editor.Posing.AutoSave;

public class PoseAutoSave : IDisposable {
	private readonly IEditorContext _ctx;
	private readonly IFramework _framework;
	
	private IPosingManager Posing => this._ctx.Posing;
	private ISceneManager Scene => this._ctx.Scene;

	private readonly Timer _timer = new();
	
	private AutoSaveConfig _cfg = null!;
	
	public PoseAutoSave(
		IEditorContext ctx,
		IFramework framework
	) {
		this._ctx = ctx;
		this._framework = framework;
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

	private void OnElapsed(object? sender, ElapsedEventArgs e) {
		if (!this.Posing.IsValid) {
			this._timer.Stop();
			return;
		}

		if (this._cfg.Enabled && this.Posing.IsEnabled)
			this._framework.RunOnFrameworkThread(this.Save);
	}

	private void Save() {
		var prefix = this._cfg.FolderFormat;
		var folder = Path.Combine(this._cfg.FilePath, prefix);

		if (!Directory.Exists(folder))
			Directory.CreateDirectory(folder);
		
		var entities = this.Scene.Children
			.Where(entity => entity is CharaEntity)
			.Cast<CharaEntity>()
			.ToList();
		
		Ktisis.Log.Info($"Auto saving poses for {entities.Count} character(s)");
		
		foreach (var chara in entities) {
			if (chara.Pose == null) continue;

			var dupeCt = 1;
			var path = Path.Combine(folder, $"{chara.Name}.pose");
			while (Path.Exists(path))
				path = Path.Combine(folder, $"{chara.Name} ({++dupeCt}).pose");

			var serializer = new JsonFileSerializer();
			var file = new EntityPoseConverter(chara.Pose).SaveFile();
			File.WriteAllText(path, serializer.Serialize(file));
		}
	}

	public void Dispose() {
		this._timer.Elapsed -= this.OnElapsed;
		this._timer.Stop();
		this._timer.Dispose();
		GC.SuppressFinalize(this);
	}
}
