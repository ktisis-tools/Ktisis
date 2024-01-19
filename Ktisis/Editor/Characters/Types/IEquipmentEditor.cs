using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Editor.Characters.State;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Types;

public interface IEquipmentEditor {
	public void ApplyStateFlagsFor(ActorEntity actor);
	
	public EquipmentModelId GetEquipIndex(ActorEntity actor, EquipIndex index);
	public void SetEquipIndex(ActorEntity actor, EquipIndex index, EquipmentModelId model);
	public void SetEquipIdVariant(ActorEntity actor, EquipIndex index, ushort id, byte variant);
	public void SetEquipStainId(ActorEntity actor, EquipIndex index, byte stainId);

	public bool GetHatVisible(ActorEntity actor);
	public void SetHatVisible(ActorEntity actor, bool visible);

	public bool GetVisorToggled(ActorEntity actor);
	public void SetVisorToggled(ActorEntity actor, bool toggled);

	public WeaponModelId GetWeaponIndex(ActorEntity actor, WeaponIndex index);
	public void SetWeaponIndex(ActorEntity actor, WeaponIndex index, WeaponModelId model);
	public void SetWeaponIdBaseVariant(ActorEntity actor, WeaponIndex index, ushort id, ushort second, byte variant);
	public void SetWeaponStainId(ActorEntity actor, WeaponIndex index, byte stainId);

	public bool GetWeaponVisible(ActorEntity actor, WeaponIndex index);
	public void SetWeaponVisible(ActorEntity actor, WeaponIndex index, bool visible);
}
