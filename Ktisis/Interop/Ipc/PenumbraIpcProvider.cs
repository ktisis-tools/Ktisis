using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;

using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using PenumbraIpc = Penumbra.Api.Ipc;

namespace Ktisis.Interop.Ipc;

// TODO: Waiting on an IPC method to disassociate spawned actors from their parent.
// This is required for collection setting to work without crashing.

public class PenumbraIpcProvider {
	private readonly FuncSubscriber<IList<string>> _getCollections;
	private readonly FuncSubscriber<int, (bool, bool, string)> _getCollectionForObject;
	private readonly FuncSubscriber<int, string, bool, bool, (PenumbraApiEc, string)> _setCollectionForObject;
    
	public PenumbraIpcProvider(
		DalamudPluginInterface dpi
	) {
		this._getCollections = PenumbraIpc.GetCollections.Subscriber(dpi);
		this._getCollectionForObject = PenumbraIpc.GetCollectionForObject.Subscriber(dpi);
		this._setCollectionForObject = PenumbraIpc.SetCollectionForObject.Subscriber(dpi);
	}

	public IEnumerable<string> GetCollections() => this._getCollections.Invoke();

	public string GetCollectionForObject(GameObject gameObject) {
		var (valid, set, collection) = this._getCollectionForObject.Invoke(gameObject.ObjectIndex);
		return collection;
	}

	public bool SetCollectionForObject(GameObject gameObject, string name) {
		Ktisis.Log.Verbose($"Setting collection for '{gameObject.Name}' ({gameObject.ObjectIndex}) to '{name}'");
		
		var (result, prev) = this._setCollectionForObject.Invoke(gameObject.ObjectIndex, name, true, true);
		
		var success = result == PenumbraApiEc.Success;
		if (!success)
			Ktisis.Log.Warning($"Penumbra collection set failed with return code: {result}");
		return success;
	}
}
