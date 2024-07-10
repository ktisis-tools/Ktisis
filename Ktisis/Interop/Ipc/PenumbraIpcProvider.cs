using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;

using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;

namespace Ktisis.Interop.Ipc;

public class PenumbraIpcProvider {
	private readonly GetCollections _getCollections;
	private readonly GetCollectionForObject _getCollectionForObject;
	private readonly SetCollectionForObject _setCollectionForObject;
	private readonly GetCutsceneParentIndex _getCutsceneParentIndex;
	private readonly SetCutsceneParentIndex _setCutsceneParentIndex;
    
	public PenumbraIpcProvider(
		IDalamudPluginInterface dpi
	) {
		this._getCollections = new GetCollections(dpi);
		this._getCollectionForObject = new GetCollectionForObject(dpi);
		this._setCollectionForObject = new SetCollectionForObject(dpi);
		this._getCutsceneParentIndex = new GetCutsceneParentIndex(dpi);
		this._setCutsceneParentIndex = new SetCutsceneParentIndex(dpi);
	}

	public Dictionary<Guid, string> GetCollections() => this._getCollections.Invoke();

	public (Guid Id, string Name) GetCollectionForObject(IGameObject gameObject) {
		var (valid, set, collection) = this._getCollectionForObject.Invoke(gameObject.ObjectIndex);
		return collection;
	}

	public bool SetCollectionForObject(IGameObject gameObject, Guid id) {
		Ktisis.Log.Verbose($"Setting collection for '{gameObject.Name}' ({gameObject.ObjectIndex}) to '{id}'");
		
		var (result, prev) = this._setCollectionForObject.Invoke(gameObject.ObjectIndex, id, true, true);
		
		var success = result == PenumbraApiEc.Success;
		if (!success)
			Ktisis.Log.Warning($"Penumbra collection set failed with return code: {result}");
		return success;
	}

	public int GetAssignedParentIndex(IGameObject gameObject) {
		return this._getCutsceneParentIndex.Invoke(gameObject.ObjectIndex);
	}

	public bool SetAssignedParentIndex(IGameObject gameObject, int index) {
		Ktisis.Log.Verbose($"Setting assigned parent for '{gameObject.Name}' ({gameObject.ObjectIndex}) to {index}");
		
		var result = this._setCutsceneParentIndex.Invoke(gameObject.ObjectIndex, index);

		var success = result == PenumbraApiEc.Success;
		if (!success)
			Ktisis.Log.Warning($"Penumbra parent set failed with return code: {result}");
		return success;
	}
}

