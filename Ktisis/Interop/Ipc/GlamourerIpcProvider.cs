using System;

using Dalamud.Plugin;

using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;

namespace Ktisis.Interop.Ipc;

public class GlamourerIpcProvider {
	private readonly GetDesignList _getDesignList;
	private readonly ApplyDesign _applyDesign;
	private readonly ApplyState _applyState;
	private readonly ApplyStateName _applyStateName;
	private readonly GetState _getState;
	private readonly RevertState _revertState;
	private readonly RevertStateName _revertStateName;
	
	public GlamourerIpcProvider(
		IDalamudPluginInterface dpi
	) {
		this._getDesignList = new GetDesignList(dpi);
		this._applyDesign = new ApplyDesign(dpi);
		this._applyState = new ApplyState(dpi);
		this._applyStateName = new ApplyStateName(dpi);
		this._getState = new GetState(dpi);
		this._revertState = new RevertState(dpi);
		this._revertStateName = new RevertStateName(dpi);
	}

	public Dictionary<Guid, string> GetDesignList() => this._getDesignList.Invoke();

	public void ApplyDesignToObject(IGameObject gameObject, Guid designId) {
		Ktisis.Log.Verbose($"Setting design for '{gameObject.Name}' ({gameObject.ObjectIndex}) to '{designId}'");

		var (result, prev) = this._applyDesign.Invoke(designId, index);
		var success = result == GlamourerApiEc.Success;
		if (!success)
			Ktisis.Log.Warning($"Glamourer design application failed with return code: {result}");

		return success;
	}

	public void ApplyState(string state, int index) {
		this._applyState.Invoke(state, index);
	}

	public void ApplyStateName(string state, string playerName) {
		this._applyStateName.Invoke(state, playerName);
	}

	public void GetState(int index) {
		this._getState.Invoke(index);
	}

	public void RevertState(int index) {
		this._revertState.Invoke(index);
	}

	public void RevertStateName(string playerName) {
		this._revertStateName.Invoke(playerName);
	}
}
