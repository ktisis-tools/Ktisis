using Dalamud.Game.ClientState.Objects.Enums;

namespace Ktisis.Editor.Characters.Types;

public interface ICustomizeEditor {
	public void SetCustomization(CustomizeIndex index, byte value);
	public byte GetCustomization(CustomizeIndex index);

	public void SetHeterochromia(bool enabled);
	public bool GetHeterochromia();
	
	public void SetEyeColor(byte value);

	public uint GetModelId();
	public void SetModelId(uint id, bool redraw = true);
	
	public void ApplyStateToGameObject();
	
	public ICustomizeBatch Prepare();
}