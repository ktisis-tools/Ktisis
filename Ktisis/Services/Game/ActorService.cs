using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Common.Extensions;
using Ktisis.Core.Attributes;

using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Ktisis.Services.Game;

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

	public IGameObject? GetIndex(int index)
		=> this._objectTable[index];
	
	public IGameObject? GetAddress(nint address)
		=> this._objectTable.CreateObjectReference(address);
	
	// Actor enumerators

	public IEnumerable<IGameObject> GetGPoseActors() {
		for (var i = GPoseIndex; i < GPoseIndex + GPoseCount; i++) {
			var actor = this.GetIndex(i);
			if (actor != null)
				yield return actor;
		}
	}

	public IEnumerable<IGameObject> GetOverworldActors() {
		var actors = this._objectTable.CharacterManagerObjects
			.Concat(this._objectTable.ClientObjects.Where(gameObject => gameObject.ObjectIndex > GPoseIndex + GPoseCount))
			.Concat(this._objectTable.StandObjects.Where(gameObject => gameObject is { ObjectKind: ObjectKind.BattleNpc or ObjectKind.EventNpc or ObjectKind.Companion or ObjectKind.MountType }));

		foreach (var actor in actors) {
			if (!actor.IsEnabled() || !actor.IsDrawing()) continue;
			yield return actor;
		}
	}

	// Skeleton wrappers

	public unsafe IGameObject? GetSkeletonOwner(Skeleton* skeleton) {
		foreach (var actor in this._objectTable) {
			var csPtr = (CSGameObject*)actor.Address;
			if (csPtr == null || csPtr->DrawObject == null) continue;

			var drawObject = csPtr->DrawObject;
			if (drawObject->Object.GetObjectType() != ObjectType.CharacterBase)
				continue;

			if (actor.GetSkeleton() == skeleton)
				return actor;
		}
		
		return null;
	}
}
