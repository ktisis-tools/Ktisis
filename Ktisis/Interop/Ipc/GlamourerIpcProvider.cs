using System;

using Dalamud.Plugin;

using Glamourer.Api.IpcSubscribers;

namespace Ktisis.Interop.Ipc;

public class GlamourerIpcProvider {
	private readonly ApplyState _applyState;
	
	public GlamourerIpcProvider(
		IDalamudPluginInterface dpi
	) {
		this._applyState = new ApplyState(dpi);
	}

	public void ApplyState(string state, int index) {
		this._applyState.Invoke(state, index);
	}
}
