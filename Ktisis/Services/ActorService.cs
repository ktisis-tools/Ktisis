using System.Collections.Generic;

using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

using Ktisis.Core.Attributes;
using Ktisis.Common.Extensions;

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
	
	// Object table wrappers

	public GameObject? GetIndex(int index)
		=> this._objectTable[index];
	
	public GameObject? GetAddress(nint address)
		=> this._objectTable.CreateObjectReference(address);
	
	// Actor enumerators

	public IEnumerable<GameObject> GetGPoseActors() {
		for (var i = GPoseIndex; i < GPoseIndex + GPoseCount; i++) {
			var actor = this.GetIndex(i);
			if (actor != null)
				yield return actor;
		}
	}

	public IEnumerable<GameObject> GetOverworldActors() {
		for (var i = 0; i < GPoseIndex - 1; i++) {
			var actor = this.GetIndex(i);
			if (actor != null && actor.IsEnabled())
				yield return actor;
		}
	}
	
	// Skeleton wrappers

	public unsafe GameObject? GetSkeletonOwner(Skeleton* skeleton) {
		foreach (var actor in this._objectTable) {
			var csPtr = (CSGameObject*)actor.Address;
			if (csPtr == null || csPtr->DrawObject == null) continue;

			var drawObject = csPtr->DrawObject;
			if (drawObject->Object.GetObjectType() != ObjectType.CharacterBase)
				continue;

			if (this.GetSkeletonFor(actor) == skeleton)
				return actor;
		}
		
		return null;
	}

	public unsafe Skeleton* GetSkeletonFor(GameObject gameObject) {
		var csPtr = (CSGameObject*)gameObject.Address;
		if (csPtr == null || csPtr->DrawObject == null)
			return null;

		var drawObject = csPtr->DrawObject;
		if (drawObject->Object.GetObjectType() != ObjectType.CharacterBase)
			return null;
		
		return ((CharacterBase*)drawObject)->Skeleton;
	}
}
