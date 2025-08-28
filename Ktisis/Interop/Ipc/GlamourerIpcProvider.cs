using System;

using Dalamud.Plugin;

using Glamourer.Api.IpcSubscribers;

namespace Ktisis.Interop.Ipc;

public class GlamourerIpcProvider {
	private readonly ApplyState _applyState;
	private readonly GetState _getState;
	
	public GlamourerIpcProvider(
		IDalamudPluginInterface dpi
	) {
		this._applyState = new ApplyState(dpi);
		this._getState = new GetState(dpi);
	}

	public void ApplyState(string state, int index) {
		this._applyState.Invoke(state, index);
	}

	public void GetState(int index) {
		this._getState.Invoke(index);
	}
}
