using System.Collections.Generic;

using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Core.Attributes;

namespace Ktisis.Services;

[Singleton]
public class ActorService {
	public const ushort GPoseIndex = 201;
	public const ushort GPoseCount = 42;
	
	private readonly IObjectTable _objectTable;

	public ActorService(
		IObjectTable objectTable
	) {
		this._objectTable = objectTable;
	}

	public GameObject? GetIndex(int index)
		=> this._objectTable[index];
	
	public GameObject? GetAddress(nint address)
		=> this._objectTable.CreateObjectReference(address);

	public IEnumerable<GameObject> GetGPoseActors() {
		for (var i = GPoseIndex; i < GPoseIndex + GPoseCount; i++) {
			var actor = this.GetIndex(i);
			if (actor != null)
				yield return actor;
		}
	}
}
