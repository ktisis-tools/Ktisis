using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Editor.Characters.State;

namespace Ktisis.Editor.Characters.Types;

public interface IEquipmentEditor {
	public void ApplyStateFlags();
	
	public EquipmentModelId GetEquipIndex(EquipIndex index);
	public void SetEquipIndex(EquipIndex index, EquipmentModelId model);
	public void SetEquipIdVariant(EquipIndex index, ushort id, byte variant);
	public void SetEquipStainId(EquipIndex index, byte stainId);

	public bool GetHatVisible();
	public void SetHatVisible(bool visible);

	public bool GetVisorToggled();
	public void SetVisorToggled(bool toggled);

	public WeaponModelId GetWeaponIndex(WeaponIndex index);
	public void SetWeaponIndex(WeaponIndex index, WeaponModelId model);
	public void SetWeaponIdBaseVariant(WeaponIndex index, ushort id, ushort second, byte variant);
	public void SetWeaponStainId(WeaponIndex index, byte stainId);

	public bool GetWeaponVisible(WeaponIndex index);
	public void SetWeaponVisible(WeaponIndex index, bool visible);
	
	public void ApplyStateToGameObject();
}
