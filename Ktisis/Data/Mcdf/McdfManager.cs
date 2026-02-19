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
using Ktisis.Services.Game;

namespace Ktisis.Data.Mcdf;

[Singleton]
public sealed class McdfManager : IDisposable {
	private readonly GPoseService _gpose;
	private readonly IFramework _framework;
	private readonly IpcManager _ipc;
	private readonly IObjectTable _objectTable;
	
	private Dictionary<IGameObject, Guid?> actors;

	public McdfManager(
		GPoseService gpose,
		IFramework framework,
		IpcManager ipc,
		IObjectTable objectTable
	) {
		this._gpose = gpose;
		this._gpose.StateChanged += this.OnGPoseEvent;
		this._gpose.Subscribe();

		this._framework = framework;
		this._ipc = ipc;
		this._objectTable = objectTable;

		this.actors = new Dictionary<IGameObject, Guid?>();
	}

	private void OnGPoseEvent(object sender, bool active) {
		if (!active) this.RevertAll();
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
		// add actor to applied list - if already there, revert them before applying mcdf
		if (this.actors.Keys.Contains(actor)) {
			Ktisis.Log.Debug($"Actor {actor.ObjectIndex} was applied this session, reverting and redrawing...");
			this.Revert(actor);
		} else
			this.actors.Add(actor, null);

		var collectionId = this.ApplyPenumbraMods(actor, data, files);
		this.ApplyGlamourerData(actor, data);
		await this.RedrawAndWait(actor);
		if (collectionId != null) {
			var ipc = this._ipc.GetPenumbraIpc();
			ipc.DeleteTemporaryCollection(collectionId.Value);
		}
		this.actors[actor] = this.ApplyCustomizeData(actor, data);
		
		Ktisis.Log.Debug("Cleaning up extracted files");
		foreach (var file in extracted.Values)
			File.Delete(file);
	}

	private Guid? ApplyCustomizeData(IGameObject actor, McdfData data) {
		var rawData = data.CustomizePlusData;
		if (!this._ipc.IsCustomizeActive) {
			if (!rawData.IsNullOrEmpty())
				Ktisis.WarningNotification("MCDF has Customize+ data, but no IPC was found!\nCheck to make sure all plugins are enabled.");
			return null;
		}

		var ipc = this._ipc.GetCustomizeIpc();
		var jsonData = !rawData.IsNullOrEmpty()
			? Encoding.UTF8.GetString(Convert.FromBase64String(rawData))
			: "{}";

		var resp = ipc.SetTemporaryProfile(actor.ObjectIndex, jsonData);
		if (resp.Id == null) Ktisis.Log.Warning($"Customize+ SetTemporaryProfile returned null Guid! status: {resp.Item1}");
		return resp.Id;
	}

	private void ApplyGlamourerData(IGameObject actor, McdfData data) {
		var glamData = data.GlamourerData;
		if (!this._ipc.IsGlamourerActive) {
			if (!glamData.IsNullOrEmpty())
				Ktisis.WarningNotification("MCDF has Glamourer data, but no IPC was found!\nCheck to make sure all plugins are enabled.");
			return;
		}
		
		var ipc = this._ipc.GetGlamourerIpc();
		ipc.ApplyState(glamData, actor.ObjectIndex);
	}

	private Guid? ApplyPenumbraMods(IGameObject actor, McdfData data, Dictionary<string, string> files) {
		if (!this._ipc.IsPenumbraActive) {
			if (files.Count != 0)
				Ktisis.WarningNotification("MCDF has Penumbra data, but no IPC was found!\nCheck to make sure all plugins are enabled.");
			return null;
		}
		
		var ipc = this._ipc.GetPenumbraIpc();
		var collectionId = ipc.CreateTemporaryCollection($"KtisisMCDF_{actor.ObjectIndex}");
		ipc.AssignTemporaryCollection(collectionId, actor.ObjectIndex);
		var id = Guid.NewGuid();
		ipc.AssignTemporaryMods(id, collectionId, files);
		ipc.AssignManipulationData(id, collectionId, data.ManipulationData);
		return collectionId;
	}

	private void RevertGlamourerData(IGameObject actor) {
		if (!this._ipc.IsGlamourerActive) return;

		var ipc = this._ipc.GetGlamourerIpc();
		ipc.RevertObject(actor);
	}

	private void DeleteGlamourerData(IGameObject actor) {
        if (this._ipc.IsGlamourerActive) {
			var ipc = this._ipc.GetGlamourerIpc();
			var res = ipc.DeleteState(actor, this._objectTable.LocalPlayer);
			if (res) return;
        }

		Ktisis.WarningNotification($"Unable to fully clear Glamourer IPC data for Actor {actor.Name.TextValue}!\nCheck /xllog for further details.");
	}

	private void RevertCustomizeData(ushort index) {
		if (!this._ipc.IsCustomizeActive) return;

		var ipc = this._ipc.GetCustomizeIpc();
		ipc.DeleteTemporaryProfile(index);
	}

	private void DeleteCustomizeData(Guid id) {
		if (!this._ipc.IsCustomizeActive) return;

		var ipc = this._ipc.GetCustomizeIpc();
		ipc.DeleteTemporaryProfileGuid(id);
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

	public async void Revert(IGameObject actor) {
		Ktisis.Log.Debug($"IPC - Revert Actor '{actor.ObjectIndex}' ...");
		this.RevertGlamourerData(actor);
		await this.RedrawAndWait(actor);
		this.RevertCustomizeData(actor.ObjectIndex);
		this.actors.Remove(actor);
	}

	public void RevertIfTouched(IGameObject actor) {
        if (!this.actors.Keys.Contains(actor)) return;
		this.RevertNoDraw(actor, this.actors[actor]);
    }

	private void RevertNoDraw(IGameObject actor, Guid? guid) {
		Ktisis.Log.Debug($"IPC - RevertNoDraw Actor '{actor.ObjectIndex}' ...");
		this.DeleteGlamourerData(actor);
		if (guid == null)
			this.RevertCustomizeData(actor.ObjectIndex);
		else
			this.DeleteCustomizeData((Guid)guid);
		this.actors.Remove(actor);
    }

	private void RevertAll() {
		// used to cleanup all remaining touched actors when leaving gpose
		foreach (var (actor, guid) in this.actors)
			this.RevertNoDraw(actor, guid);

		// empty actor list for next session
		this.actors.Clear();
		this.actors.TrimExcess();

		// free up glam locks
		if (!this._ipc.IsGlamourerActive) return;
		this._ipc.GetGlamourerIpc().Unlock();
	}

	// IDisposable

	public void Dispose() {
		Ktisis.Log.Info("Disposing MCDF manager.");

		var temp = GetTempPath(create: false);
		if (Directory.Exists(temp))
			Directory.Delete(temp, true);

		this._gpose.StateChanged -= this.OnGPoseEvent;
	}
}
