using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;

namespace Ktisis.Common.Extensions;

public static class GameObjectEx {
	public static string GetNameOrFallback(this GameObject gameObject) {
		var name = gameObject.Name.TextValue;
		return !name.IsNullOrEmpty() ? name : $"Actor #{gameObject.ObjectIndex}";
	}

	public unsafe static bool IsEnabled(this GameObject gameObject) {
		var csActor = (CSGameObject*)gameObject.Address;
		if (csActor == null) return false;
		return (csActor->RenderFlags & 2) == 0;
	}
	
	public unsafe static void SetWorld(this GameObject gameObject, ushort world) {
		var charaPtr = (Character*)gameObject.Address;
		if (charaPtr == null || !charaPtr->GameObject.IsCharacter()) return;
		charaPtr->CurrentWorld = world;
		charaPtr->HomeWorld = world;
	}
	
	public unsafe static void SetTargetable(this GameObject gameObject, bool targetable) {
		var charaPtr = (CSGameObject*)gameObject.Address;
		if (charaPtr == null) return;
		
		if (targetable)
			charaPtr->TargetableStatus |= ObjectTargetableFlags.IsTargetable;
		else
			charaPtr->TargetableStatus &= ~ObjectTargetableFlags.IsTargetable;
	}
}
