using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;

namespace Ktisis.Interop.Ipc;

public class GlamourerIpcProvider {
	private readonly GetDesignList _getDesignList;
	private readonly ApplyDesign _applyDesign;
	private readonly ApplyState _applyState;
	private readonly RevertState _revertState;
	private readonly RevertStateName _revertStateName;
	private readonly UnlockState _unlockState;
	private readonly UnlockStateName _unlockStateName;
	private readonly UnlockAll _unlockAll;
	private readonly uint Key = 0x0001F407;
	
	public GlamourerIpcProvider(
		IDalamudPluginInterface dpi
	) {
		this._getDesignList = new GetDesignList(dpi);
		this._applyState = new ApplyState(dpi);
		this._revertState = new RevertState(dpi);
		this._revertStateName = new RevertStateName(dpi);
		this._applyDesign = new ApplyDesign(dpi);
		this._unlockState = new UnlockState(dpi);
		this._unlockStateName = new UnlockStateName(dpi);
		this._unlockAll = new UnlockAll(dpi);
	}

	public Dictionary<Guid, string> GetDesignList() => this._getDesignList.Invoke();

	public bool ApplyDesignToObject(IGameObject gameObject, Guid designId) {
		Ktisis.Log.Debug($"Setting design for '{gameObject.Name}' ({gameObject.ObjectIndex}) to '{designId}'");
		var result = this._applyDesign.Invoke(designId, gameObject.ObjectIndex, 0);
		var success = result == GlamourerApiEc.Success;
		if (!success)
			Ktisis.Log.Warning($"Glamourer design application failed with return code: {result}");

		return success;
	}

	public bool RevertObject(IGameObject gameObject) {
		Ktisis.Log.Debug($"Reverting state for '{gameObject.Name}' ({gameObject.ObjectIndex})");

		// unlock object before reverting so we never relock it when we revert it
		this.UnlockObject(gameObject);

		var result = this.RevertStateName(gameObject.Name.TextValue);
		if (result != GlamourerApiEc.Success) {
			Ktisis.Log.Warning($"Glamourer revert failed with return code: {result}, trying by index...");
			result = this.RevertState(gameObject.ObjectIndex);
		}

		return result == GlamourerApiEc.Success;
	}

	public void ApplyState(string state, int index) {
		this._applyState.Invoke(state, index, Key);
	}

	public void Unlock() => this._unlockAll.Invoke(Key);

	private void UnlockObject(IGameObject gameObject) {
        var res = this._unlockState.Invoke(gameObject.ObjectIndex, Key);
		if (res != GlamourerApiEc.Success) this._unlockStateName.Invoke(gameObject.Name.TextValue, Key);
    }

	private GlamourerApiEc RevertState(int index) {
		return this._revertState.Invoke(index);
	}

	private GlamourerApiEc RevertStateName(string playerName) {
		return this._revertStateName.Invoke(playerName);
	}
}
