using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Structs.Characters;

namespace Ktisis.GameData.Excel.Types;

public interface INpcBase {
	public string Name { get; set;  }

	public ushort GetModelId() => 0;

	public CustomizeContainer? GetCustomize() => null;
	public EquipmentContainer? GetEquipment() => null;

	public WeaponModelId? GetMainHand() => null;
	public WeaponModelId? GetOffHand() => null;
}
