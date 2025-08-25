using System;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;

namespace Ktisis.Interop.Ipc;

public class PenumbraIpcProvider {
	private readonly GetCollections _getCollections;
	private readonly GetCollectionForObject _getCollectionForObject;
	private readonly SetCollectionForObject _setCollectionForObject;
	private readonly GetCutsceneParentIndex _getCutsceneParentIndex;
	private readonly SetCutsceneParentIndex _setCutsceneParentIndex;
	private readonly AssignTemporaryCollection _assignTemporaryCollection;
	private readonly ICallGateSubscriber<string, string, (PenumbraApiEc, Guid Guid)> _createTemporaryCollection;
	private readonly DeleteTemporaryCollection _deleteTemporaryCollection;
	private readonly AddTemporaryMod _addTemporaryMod;
	private readonly RemoveTemporaryMod _removeTemporaryMod;
	private readonly RedrawObject _redrawObject;
    
	public PenumbraIpcProvider(
		IDalamudPluginInterface dpi
	) {
		this._getCollections = new GetCollections(dpi);
		this._getCollectionForObject = new GetCollectionForObject(dpi);
		this._setCollectionForObject = new SetCollectionForObject(dpi);
		this._getCutsceneParentIndex = new GetCutsceneParentIndex(dpi);
		this._setCutsceneParentIndex = new SetCutsceneParentIndex(dpi);
		this._assignTemporaryCollection = new AssignTemporaryCollection(dpi);
		//this._createTemporaryCollection = new CreateTemporaryCollection(dpi);
		this._createTemporaryCollection = dpi.GetIpcSubscriber<string, string, (PenumbraApiEc, Guid Guid)>("Penumbra.CreateTemporaryCollection.V6");
		this._deleteTemporaryCollection = new DeleteTemporaryCollection(dpi);
		this._addTemporaryMod = new AddTemporaryMod(dpi);
		this._removeTemporaryMod = new RemoveTemporaryMod(dpi);
		this._redrawObject = new RedrawObject(dpi);
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

	public void AssignTemporaryCollection(Guid collectionId, int actorIndex) {
		this._assignTemporaryCollection.Invoke(collectionId, actorIndex);
	}

	public Guid CreateTemporaryCollection(string name) {
		return this._createTemporaryCollection.InvokeFunc("Ktisis", name).Guid;
	}

	public void DeleteTemporaryCollection(Guid collectionId) {
		this._deleteTemporaryCollection.Invoke(collectionId);
	}

	public bool SetAssignedParentIndex(IGameObject gameObject, int index) {
		Ktisis.Log.Verbose($"Setting assigned parent for '{gameObject.Name}' ({gameObject.ObjectIndex}) to {index}");
		
		var result = this._setCutsceneParentIndex.Invoke(gameObject.ObjectIndex, index);

		var success = result == PenumbraApiEc.Success;
		if (!success)
			Ktisis.Log.Warning($"Penumbra parent set failed with return code: {result}");
		return success;
	}

	public void AssignTemporaryMods(Guid id, Guid collectionId, Dictionary<string, string> paths) {
		var rem = this._removeTemporaryMod.Invoke("MareChara_Files", collectionId, 0);
		var add = this._addTemporaryMod.Invoke("MareChara_Files", collectionId, paths, string.Empty, 0);
		Ktisis.Log.Info($"{rem} {add}");
	}

	public void AssignManipulationData(Guid id, Guid collectionId, string manipData) {
		this._addTemporaryMod.Invoke("MareChara_Meta", collectionId, [], manipData, 0);
	}

	public void Redraw(int index) => this._redrawObject.Invoke(index);
}
