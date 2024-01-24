using Dalamud.Game.ClientState.Objects.Enums;

namespace Ktisis.Editor.Characters.Types;

public interface ICustomizeEditor {
	public void SetCustomization(CustomizeIndex index, byte value);
	public byte GetCustomization(CustomizeIndex index);

	public void SetHeterochromia(bool enabled);
	public bool GetHeterochromia();
	
	public void SetEyeColor(byte value);
	
	public void ApplyStateToGameObject();
	
	public ICustomizeBatch Prepare();
}

public interface ICustomizeBatch {
	public ICustomizeBatch SetCustomization(CustomizeIndex index, byte value);

	public ICustomizeBatch SetIfNotNull(CustomizeIndex index, byte? value);
	
	public void Apply();
}