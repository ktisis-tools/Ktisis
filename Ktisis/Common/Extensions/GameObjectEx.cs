using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Utility;

using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace Ktisis.Common.Extensions;

public static class GameObjectEx {
	public static string GetNameOrFallback(this GameObject gameObject) {
		var name = gameObject.Name.TextValue;
		return !name.IsNullOrEmpty() ? name : $"Actor #{gameObject.ObjectIndex}";
	}

	public unsafe static bool IsEnabled(this GameObject gameObject) {
		var csActor = (CSGameObject*)gameObject.Address;
		if (csActor == null)
			return false;
		return (csActor->RenderFlags & 2) == 0;
	}
}
