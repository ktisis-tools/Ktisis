using Ktisis.Interface.Components;

using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using Ktisis.Helpers;
using Ktisis.Structs.Actor;
using Ktisis.Structs.Poses;

namespace Ktisis.Util
{
	internal class PoseAutoSave
	{
		private Queue<string> prefixes = new();
		private Timer? _timer = null;
		private string SaveFolder => Ktisis.Configuration.AutoSavePath;

		public void Enable()
		{
			if (!Ktisis.Configuration.EnableAutoSave)
				return;

			if (!Directory.Exists(SaveFolder))
				Directory.CreateDirectory(SaveFolder);

			_timer = new Timer(TimeSpan.FromSeconds(Ktisis.Configuration.AutoSaveInterval));
			_timer.Elapsed += Save;
			_timer.AutoReset = true;

			_timer.Start();
			prefixes.Clear();
		}

		public void Disable()
		{
			_timer?.Stop();

			if (!Ktisis.Configuration.ClearAutoSavesOnExit || Ktisis.Configuration.AutoSaveCount <= 0) return;

			while (prefixes.Count > 0)
			{
				DeleteOldest();
			}
		}

		private void Save(object? sender, ElapsedEventArgs e)
		{
			if (!Ktisis.IsInGPose)
			{
				Disable();
				return;
			}

			var actors = ActorsList.GetSelectorList();

			Dalamud.Logging.PluginLog.LogInformation($"Saving {actors.Count} actors");

			var prefix = $"AutoSave - {DateTime.Now:HH-mm-ss}";
			var folder = Path.Combine(SaveFolder, prefix);
			prefixes.Enqueue(prefix);

			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);

			unsafe
			{
				foreach (var actorPtr in actors)
				{
					var actor = (Actor*)actorPtr;
					var filename = $"{actor->GetNameOrId()}.pose";
					Dalamud.Logging.PluginLog.LogInformation($"Saving {filename}");

					var path = Path.Combine(folder, filename);
					Dalamud.Logging.PluginLog.LogVerbose($"Saving to {path}");

					PoseHelpers.ExportPose(actor, path, PoseMode.All);
				}

			}

			Dalamud.Logging.PluginLog.LogVerbose($"Prefix count: {prefixes.Count} max: {Ktisis.Configuration.AutoSaveCount}");

			//Clear old saves
			while (prefixes.Count > Ktisis.Configuration.AutoSaveCount)
			{
				DeleteOldest();
			}

			//Dynamically update the interval.

			if (_timer != null && Math.Abs(_timer.Interval - TimeSpan.FromSeconds(Ktisis.Configuration.AutoSaveInterval).TotalMilliseconds) > 0.01)
				_timer.Interval = TimeSpan.FromSeconds(Ktisis.Configuration.AutoSaveInterval).TotalMilliseconds;
		}

		private void DeleteOldest()
		{
			var oldest = prefixes.Dequeue();
			var folder = Path.Combine(SaveFolder, oldest);
			if (Directory.Exists(folder))
			{
				Dalamud.Logging.PluginLog.LogVerbose($"Deleting {folder}");
				Directory.Delete(folder, true);
			}
		}
	}
}
