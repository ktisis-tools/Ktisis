using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Structs.Character;

namespace Ktisis.Editor.Strategy.Decor;

public interface ICharacter {
	public unsafe CharacterBase* GetCharacter();
	
	public Customize? GetCustomize();
	public EquipmentModelId[]? GetEquipment();
}
