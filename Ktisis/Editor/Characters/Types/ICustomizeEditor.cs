using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Types;

public interface ICustomizeEditor {
	public byte GetCustomization(ActorEntity actor, CustomizeIndex index);
	public void SetCustomization(ActorEntity actor, CustomizeIndex index, byte value);
}
