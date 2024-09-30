using System;
using System.Collections.Generic;

using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace Ktisis.Interop.Ipc;

using IPCProfileDataTuple = (Guid UniqueId, string Name, string VirtualPath, string CharacterName, bool IsEnabled);

public class CustomizeIpcProvider {
	private readonly ICallGateSubscriber<IList<IPCProfileDataTuple>> _getProfileList;
	private readonly ICallGateSubscriber<ushort, (int, Guid?)> _getActiveProfileId;
	private readonly ICallGateSubscriber<Guid, (int, string?)> _getProfileByUId;
	private readonly ICallGateSubscriber<ushort, string, (int, Guid?)> _setTemporaryProfile;
	private readonly ICallGateSubscriber<ushort, int> _unsetTemporaryProfile;
	
	public CustomizeIpcProvider(
		IDalamudPluginInterface dpi
	) {
		this._getProfileList = dpi.GetIpcSubscriber<IList<IPCProfileDataTuple>>("CustomizePlus.Profile.GetList");
		this._getActiveProfileId = dpi.GetIpcSubscriber<ushort, (int, Guid?)>("CustomizePlus.Profile.GetActiveProfileIdOnCharacter");
		this._getProfileByUId = dpi.GetIpcSubscriber<Guid, (int, string?)>("CustomizePlus.Profile.GetByUniqueId");
		this._setTemporaryProfile = dpi.GetIpcSubscriber<ushort, string, (int, Guid?)>("CustomizePlus.Profile.SetTemporaryProfileOnCharacter");
		this._unsetTemporaryProfile = dpi.GetIpcSubscriber<ushort, int>("CustomizePlus.Profile.DeleteTemporaryProfileOnCharacter");
	}

	public IList<IPCProfileDataTuple> GetProfileList() => this._getProfileList.InvokeFunc();

	public (int, Guid? Id) GetActiveProfileId(ushort gameObjectIndex) => this._getActiveProfileId.InvokeFunc(gameObjectIndex);

	public (int, string? Data) GetProfileByUniqueId(Guid id) => this._getProfileByUId.InvokeFunc(id);

	public (int, Guid? Id) SetTemporaryProfile(ushort gameObjectIndex, string profileJson) => this._setTemporaryProfile.InvokeFunc(gameObjectIndex, profileJson);

	public int DeleteTemporaryProfile(ushort gameObjectIndex) => this._unsetTemporaryProfile.InvokeFunc(gameObjectIndex);
}
