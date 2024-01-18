using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Editor.Characters.Data;
using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Handlers;

public class EquipmentEditor : IEquipmentEditor {
	public void ApplyStateFlagsFor(ActorEntity entity) {
		this.UpdateWeaponVisibleState(entity, WeaponIndex.MainHand);
		this.UpdateWeaponVisibleState(entity, WeaponIndex.OffHand);
		
		if (entity.Appearance.VisorToggled != EquipmentToggle.None)
			this.SetVisorToggled(entity, entity.Appearance.VisorToggled == EquipmentToggle.On);
	}
	
	private void SetStateIfNotTracked(ActorEntity actor, EquipIndex index) {
		if (!actor.IsValid || actor.Appearance.Equipment.IsSet(index)) return;
		actor.Appearance.Equipment[index] = this.GetEquipIndex(actor, index);
	}
	
	// Equipment wrappers

	public unsafe EquipmentModelId GetEquipIndex(ActorEntity actor, EquipIndex index) {
		if (!actor.IsValid) return default;
		if (actor.Appearance.Equipment.IsSet(index))
			return actor.Appearance.Equipment[index];
		return actor.CharacterBaseEx != null ? actor.CharacterBaseEx->Equipment[(uint)index] : default;
	}
	
	public unsafe void SetEquipIndex(ActorEntity actor, EquipIndex index, EquipmentModelId model) {
		if (!actor.IsValid) return;
		actor.Appearance.Equipment[index] = model;
		var chara = actor.GetCharacter();
		if (chara != null) chara->FlagSlotForUpdate((uint)index, &model);
	}

	public void SetEquipIdVariant(ActorEntity actor, EquipIndex index, ushort id, byte variant) {
		var model = this.GetEquipIndex(actor, index);
		model.Id = id;
		model.Variant = variant;
		this.SetEquipIndex(actor, index, model);
	}

	public void SetEquipStainId(ActorEntity actor, EquipIndex index, byte stainId) {
		var model = this.GetEquipIndex(actor, index);
		model.Stain = stainId;
		this.SetEquipIndex(actor, index, model);
	}

	private unsafe void ForceUpdateEquipIndex(ActorEntity actor, EquipIndex index) {
		if (!actor.IsValid) return;
		
		var chara = actor.GetCharacter();
		if (chara == null) return;
		
		var current = this.GetEquipIndex(actor, index);
		chara->FlagSlotForUpdate((uint)index, &current);
	}
	
	// Hat visibility

	public unsafe bool GetHatVisible(ActorEntity actor) => actor.IsValid
		&& actor.Character != null
		&& actor.Appearance.CheckHatVisible(!actor.Character->DrawData.IsHatHidden);

	public unsafe void SetHatVisible(ActorEntity actor, bool visible) {
		if (!actor.IsValid || actor.Character == null) return;
		this.SetStateIfNotTracked(actor, EquipIndex.Head);
		actor.Appearance.HatVisible = visible ? EquipmentToggle.On : EquipmentToggle.Off;
		actor.Character->DrawData.HideHeadgear(0, !visible);
		if (visible) this.ForceUpdateEquipIndex(actor, EquipIndex.Head);
	}
	
	// Visor toggle

	public unsafe bool GetVisorToggled(ActorEntity actor) => actor.IsValid
		&& actor.Character != null
		&& actor.Appearance.CheckVisorToggled(actor.Character->DrawData.IsVisorToggled);

	public unsafe void SetVisorToggled(ActorEntity actor, bool toggled) {
		if (!actor.IsValid || actor.Character == null) return;
		actor.Appearance.VisorToggled = toggled ? EquipmentToggle.On : EquipmentToggle.Off;
		actor.Character->DrawData.SetVisor(toggled);
	}
	
	// Weapon wrappers

	public unsafe WeaponModelId GetWeaponIndex(ActorEntity actor, WeaponIndex index) {
		if (!actor.IsValid) return default;
		if (actor.Appearance.Weapons.IsSet(index))
			return actor.Appearance.Weapons[index];
		var data = GetWeaponData(actor, index);
		return data != null ? data->ModelId : default;
	}

	public unsafe void SetWeaponIndex(ActorEntity actor, WeaponIndex index, WeaponModelId model) {
		if (!actor.IsValid) return;
		actor.Appearance.Weapons[index] = model;
		var chara = actor.Character;
		if (chara != null)
			chara->DrawData.LoadWeapon((DrawDataContainer.WeaponSlot)index, model, 0, 0, 0, 0);
	}

	public void SetWeaponIdBaseVariant(ActorEntity actor, WeaponIndex index, ushort id, ushort second, byte variant) {
		var model = this.GetWeaponIndex(actor, index);
		model.Id = id;
		model.Type = second;
		model.Variant = variant;
		this.SetWeaponIndex(actor, index, model);
	}

	public void SetWeaponStainId(ActorEntity actor, WeaponIndex index, byte stainId) {
		var model = this.GetWeaponIndex(actor, index);
		model.Stain = stainId;
		this.SetWeaponIndex(actor, index, model);
	}
	
	private unsafe static DrawObjectData* GetWeaponData(ActorEntity actor, WeaponIndex index) {
		if (!actor.IsValid || actor.Character == null) return null;
		return (DrawObjectData*)actor.Character->DrawData.WeaponData + (uint)index;
	}
	
	// Weapon visible

	public unsafe bool GetWeaponVisible(ActorEntity actor, WeaponIndex index) {
		var data = GetWeaponData(actor, index);
		return data != null && actor.Appearance.Weapons.CheckVisible(index, !data->IsHidden);
	}

	public unsafe void SetWeaponVisible(ActorEntity actor, WeaponIndex index, bool visible) {
		actor.Appearance.Weapons.SetVisible(index, visible);
		var data = GetWeaponData(actor, index);
		if (data != null) data->IsHidden = !visible;
	}

	private void UpdateWeaponVisibleState(ActorEntity actor, WeaponIndex index) {
		var state = actor.Appearance.Weapons.GetVisible(index);
		if (state != EquipmentToggle.None)
			this.SetWeaponVisible(actor, index, state == EquipmentToggle.On);
	}
}
