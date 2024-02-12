using Dalamud.Game.ClientState.Objects.Enums;

namespace Ktisis.Editor.Characters.Types;

public interface ICustomizeBatch {
	public ICustomizeBatch SetCustomization(CustomizeIndex index, byte value);

	public ICustomizeBatch SetIfNotNull(CustomizeIndex index, byte? value);
	
	public ICustomizeBatch SetModelId(uint id);
	
	public void Apply();
}
