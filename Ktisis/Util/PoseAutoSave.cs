using System;
using System.IO;
using System.Timers;
using System.Collections.Generic;
using System.Linq;

using Ktisis.Helpers;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Poses;
using Ktisis.Interface.Components;
using Ktisis.Interop.Hooks;

namespace Ktisis.Util {
	internal class PoseAutoSave {
		private Queue<string> prefixes = new();
		private Timer? _timer = null;
		private string SaveFolder => Ktisis.Configuration.AutoSavePath;

		public void Enable() {
			if (!Ktisis.Configuration.EnableAutoSave)
				return;

			if (!Directory.Exists(SaveFolder))
				Directory.CreateDirectory(SaveFolder);

			_timer = new Timer(TimeSpan.FromSeconds(Ktisis.Configuration.AutoSaveInterval));
			_timer.Elapsed += OnElapsed;
			_timer.AutoReset = true;

			_timer.Start();
			prefixes.Clear();
		}

		public void Disable() {
			lock (this) {
				_timer?.Stop();
				
				if (!Ktisis.Configuration.ClearAutoSavesOnExit || Ktisis.Configuration.AutoSaveCount <= 0)
					return;

				while (prefixes.Count > 0) {
					DeleteOldest();
				}
			}
		}

		private void OnElapsed(object? sender, ElapsedEventArgs e) {
			Services.Framework.RunOnFrameworkThread(Save);
		}

		internal void UpdateSettings() {
			var timerEnabled = _timer?.Enabled ?? false;
			var cfg = Ktisis.Configuration;

			if (!timerEnabled && cfg.EnableAutoSave && PoseHooks.PosingEnabled)
				Enable();
			else if (timerEnabled && !cfg.EnableAutoSave && PoseHooks.PosingEnabled)
				Disable();

			if (_timer is not null && Math.Abs(_timer.Interval - TimeSpan.FromSeconds(cfg.AutoSaveInterval).TotalMilliseconds) > 0.01)
				_timer.Interval = TimeSpan.FromSeconds(cfg.AutoSaveInterval).TotalMilliseconds;
		}

		private void Save() {
			if (!Ktisis.IsInGPose) {
				Disable();
				return;
			}

			var actors = ActorsList.SavedObjects;

			Logger.Information($"Saving {actors.Count} actors");

			// var prefix = $"AutoSave - {DateTime.Now:yyyy-MM-dd HH-mm-ss}";
			var prefix = PathHelper.Replace(Ktisis.Configuration.AutoSaveFormat);
			var folder = Path.Combine(SaveFolder, prefix);
			prefixes.Enqueue(prefix);

			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);

			unsafe {
				foreach (var actorPtr in actors) {
					var actor = (Actor*)actorPtr;
					var filename = $"{actor->GetNameOrId()}.pose";
					Logger.Information($"Saving {filename}");

					var path = Path.Combine(folder, filename);
					Logger.Verbose($"Saving to {path}");

					PoseHelpers.ExportPose(actor, path, PoseMode.All);
				}
			}

			Logger.Verbose($"Prefix count: {prefixes.Count} max: {Ktisis.Configuration.AutoSaveCount}");

			//Clear old saves
			while (prefixes.Count > Ktisis.Configuration.AutoSaveCount) {
				DeleteOldest();
			}
		}

		private void DeleteOldest() {
			var oldest = prefixes.Dequeue();
			var folder = Path.Combine(SaveFolder, oldest);
			if (Directory.Exists(folder)) {
				Logger.Verbose($"Deleting {folder}");
				Directory.Delete(folder, true);
			}
			
			DeleteEmptyDirs(SaveFolder);
		}
		
		static void DeleteEmptyDirs(string dir)
		{
			if (string.IsNullOrEmpty(dir))
				throw new ArgumentException(
					"Starting directory is a null reference or an empty string", 
					nameof(dir));

			try
			{
				foreach (var d in Directory.EnumerateDirectories(dir))
				{
					DeleteEmptyDirs(d);
				}

				var entries = Directory.EnumerateFileSystemEntries(dir);

				if (entries.Any())
					return;
			
				try
				{
					Directory.Delete(dir);
				}
				catch (UnauthorizedAccessException) { }
				catch (DirectoryNotFoundException) { }
			}
			catch (UnauthorizedAccessException) { }
		}
	}
}
