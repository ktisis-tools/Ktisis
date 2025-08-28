using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Ktisis.Common.Extensions;

using Ktisis.Core.Attributes;
using Ktisis.Interop.Ipc;

namespace Ktisis.Data.Mcdf;

[Singleton]
public sealed class McdfManager : IDisposable {
	private readonly IFramework _framework;
	private readonly IpcManager _ipc;
	private List<IGameObject> actors;
	

	public McdfManager(
		IFramework framework,
		IpcManager ipc
	) {
		this._framework = framework;
		this._ipc = ipc;
		this.actors = new();
	}
	
	// MCDF loading

	public void LoadAndApplyTo(string path, IGameObject actor) {
		_ = this.LoadAndApplyToAsync(path, actor).ContinueWith(task => {
			if (task.Exception != null)
				Ktisis.Log.Error($"Failed to load MCDF:\n{task.Exception.InnerException}");
		}, TaskContinuationOptions.OnlyOnFaulted);
	}

	private async Task LoadAndApplyToAsync(string path, IGameObject actor) {
		using var reader = McdfReader.FromPath(path);
		
		var temp = GetTempPath(create: true);
		
		Ktisis.Log.Debug("Reading and extracting MCDF file");
		var data = reader.GetData();
		var extracted = reader.Extract(temp);

		var files = extracted.ToDictionary();
		foreach (var entry in data.FileSwaps.SelectMany(x => x.GamePaths, (k, p) => (GamePath: p, FilePath: k.FileSwapPath)))
			files[entry.GamePath] = entry.FilePath;
		
		Ktisis.Log.Debug("Applying MCDF data");
		var collectionId = this.ApplyPenumbraMods(actor, data, files);
		this.ApplyGlamourerData(actor, data);
		await this.RedrawAndWait(actor);
		if (collectionId != null) {
			var ipc = this._ipc.GetPenumbraIpc();
			ipc.DeleteTemporaryCollection(collectionId.Value);
		}
		this.ApplyCustomizeData(actor, data);
		
		Ktisis.Log.Debug("Cleaning up extracted files");
		foreach (var file in extracted.Values)
			File.Delete(file);

		// add actor to applied list
		this.actors.add(actor);
	}

	private void ApplyCustomizeData(IGameObject actor, McdfData data) {
		if (!this._ipc.IsCustomizeActive) return;
		
		var ipc = this._ipc.GetCustomizeIpc();
		var rawData = data.CustomizePlusData;
		var jsonData = !rawData.IsNullOrEmpty()
			? Encoding.UTF8.GetString(Convert.FromBase64String(rawData))
			: "{}";
		Ktisis.Log.Info(jsonData);
		ipc.SetTemporaryProfile(actor.ObjectIndex, jsonData);
	}

	private void RevertCustomizeData(IGameObject actor) {
		if (!this._ipc.IsCustomizeActive) return;

		var ipc = this._ipc.GetCustomizeIpc();
		ipc.DeleteTemporaryProfile(actor.ObjectIndex);
	}

	private void ApplyGlamourerData(IGameObject actor, McdfData data) {
		if (!this._ipc.IsGlamourerActive) return;
		
		var ipc = this._ipc.GetGlamourerIpc();
		ipc.ApplyState(data.GlamourerData, actor.ObjectIndex);
	}

	private void RevertGlamourerData(IGameObject actor) {
		if (!this._ipc.IsGlamourerActive) return;

		var ipc = this._ipc.GetGlamourerIpc();
		ipc.RevertState(actor.ObjectIndex);
	}

	private Guid? ApplyPenumbraMods(IGameObject actor, McdfData data, Dictionary<string, string> files) {
		if (!this._ipc.IsPenumbraActive) return null;
		
		var ipc = this._ipc.GetPenumbraIpc();
		var collectionId = ipc.CreateTemporaryCollection($"KtisisMCDF_{actor.ObjectIndex}");
		ipc.AssignTemporaryCollection(collectionId, actor.ObjectIndex);
		var id = Guid.NewGuid();
		ipc.AssignTemporaryMods(id, collectionId, files);
		ipc.AssignManipulationData(id, collectionId, data.ManipulationData);
		return collectionId;
	}

	private async Task RedrawAndWait(IGameObject actor) {
		actor.Redraw();
		
		var start = DateTime.Now;
		do {
			var isDrawing = await this._framework.RunOnFrameworkThread(actor.IsDrawing);
			if (isDrawing) return;

			await Task.Delay(100);
		} while (actor.IsValid() && (DateTime.Now - start).TotalMilliseconds < 20000);
		
		Ktisis.Log.Warning($"Timed out waiting for '{actor.Name}' to redraw!");
	}
	
	// Temp path

	private static string GetTempPath(bool create) {
		var path = Path.Join(Path.GetTempPath(), "Ktisis");
		if (create && !Directory.Exists(path)) Directory.CreateDirectory(path);
		return path;
	}
	
	
	// IDisposable
	
	public void Dispose() {
		Ktisis.Log.Info("Disposing MCDF manager.");

		// cleanup all touched actors
		foreach (var actor in this.actors) {
			Ktisis.Log.Info($"IPC - reverting actor '{actor.Name}' ...");
			this.RevertGlamourerData(actor);
			this.RevertCustomizeData(actor);
		}

		var temp = GetTempPath(create: false);
		if (Directory.Exists(temp))
			Directory.Delete(temp, true);
	}
}
