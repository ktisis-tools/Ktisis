using System.Linq;
using System.Text;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Ktisis.Common.Extensions;

public static class GameObjectEx {
	public static string GetNameOrFallback(this IGameObject gameObject) {
		var name = gameObject.Name.TextValue;
		return !name.IsNullOrEmpty() ? name : $"Actor #{gameObject.ObjectIndex}";
	}

	public unsafe static DrawObject* GetDrawObject(this IGameObject gameObject) {
		var csPtr = (CSGameObject*)gameObject.Address;
		return csPtr != null ? csPtr->DrawObject : null;
	}

	public unsafe static Skeleton* GetSkeleton(this IGameObject gameObject) {
		var csPtr = (CSGameObject*)gameObject.Address;
		if (csPtr == null || csPtr->DrawObject == null)
			return null;

		var drawObject = csPtr->DrawObject;
		if (drawObject->Object.GetObjectType() != ObjectType.CharacterBase)
			return null;
		
		return ((CharacterBase*)drawObject)->Skeleton;
	}

	public unsafe static bool IsEnabled(this IGameObject gameObject) {
		var csActor = (CSGameObject*)gameObject.Address;
		if (csActor == null) return false;
		return (csActor->RenderFlags & 2) == 0;
	}
	
	public unsafe static void SetWorld(this IGameObject gameObject, ushort world) {
		var charaPtr = (Character*)gameObject.Address;
		if (charaPtr == null || !charaPtr->GameObject.IsCharacter()) return;
		charaPtr->CurrentWorld = world;
		charaPtr->HomeWorld = world;
	}
	
	public unsafe static void SetName(this IGameObject gameObject, string name) {
		var gameObjectPtr = (CSGameObject*)gameObject.Address;
		if (gameObjectPtr == null) return;

		var bytes = Encoding.UTF8.GetBytes(name).Append((byte)0).ToArray();
		for (var i = 0; i < bytes.Length; i++)
			gameObjectPtr->Name[i] = bytes[i];
	}
	
	public unsafe static void SetTargetable(this IGameObject gameObject, bool targetable) {
		var charaPtr = (CSGameObject*)gameObject.Address;
		if (charaPtr == null) return;
		
		if (targetable)
			charaPtr->TargetableStatus |= ObjectTargetableFlags.IsTargetable;
		else
			charaPtr->TargetableStatus &= ~ObjectTargetableFlags.IsTargetable;
	}

	public unsafe static void SetGPoseTarget(this IGameObject gameObject) {
		if (!gameObject.IsValid()) return;

		var target = TargetSystem.Instance();
		if (target == null || target->GPoseTarget == null) return;

		target->GPoseTarget = (CSGameObject*)gameObject.Address;
	}
}