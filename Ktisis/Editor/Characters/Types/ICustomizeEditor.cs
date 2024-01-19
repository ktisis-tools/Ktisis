using Dalamud.Game.ClientState.Objects.Enums;

using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Types;

public interface ICustomizeEditor {
	public ushort GetDataId(ActorEntity actor);
	
	public void SetCustomization(ActorEntity actor, CustomizeIndex index, byte value);
	public byte GetCustomization(ActorEntity actor, CustomizeIndex index);

	public ICustomizeBatch Prepare();
}

public interface ICustomizeBatch {
	public ICustomizeBatch SetCustomization(CustomizeIndex index, byte value);
	
	public void Dispatch(ActorEntity actor);
}