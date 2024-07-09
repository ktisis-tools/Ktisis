using System;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Editor.Characters.State;
using Ktisis.Editor.Characters.Types;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters.Handlers;

public class EquipmentEditor(ActorEntity actor) : IEquipmentEditor {
	// State flags
	
	public void ApplyStateFlags() {
		this.UpdateWeaponVisibleState(WeaponIndex.MainHand);
		this.UpdateWeaponVisibleState(WeaponIndex.OffHand);
		
		if (actor.Appearance.VisorToggled != EquipmentToggle.None)
			this.SetVisorToggled(actor.Appearance.VisorToggled == EquipmentToggle.On);
	}
	
	private void SetStateIfNotTracked(EquipIndex index) {
		if (!actor.IsValid || actor.Appearance.Equipment.IsSet(index)) return;
		actor.Appearance.Equipment[index] = this.GetEquipIndex(index);
	}
	
	// Equipment wrappers

	public unsafe EquipmentModelId GetEquipIndex(EquipIndex index) {
		if (!actor.IsValid) return default;
		if (actor.Appearance.Equipment.IsSet(index))
			return actor.Appearance.Equipment[index];
		return actor.CharacterBaseEx != null ? actor.CharacterBaseEx->Equipment[(uint)index] : default;
	}

	private unsafe delegate bool FlagSlotDelegate(nint a1, EquipIndex a2, EquipmentModelId* a3);
	
	public unsafe void SetEquipIndex(EquipIndex index, EquipmentModelId model) {
		if (!actor.IsValid) return;
		actor.Appearance.Equipment[index] = model;
		var chara = actor.GetCharacter();
		if (chara != null) {
			// TODO: Fix after CS#1027 gets merged.
			Marshal.GetDelegateForFunctionPointer<FlagSlotDelegate>(
				((nint*)chara->VirtualTable)[69]
			)((nint)chara, index, &model);
			//chara->FlagSlotForUpdate((uint)index, &model);
		}
	}

	public void SetEquipIdVariant(EquipIndex index, ushort id, byte variant) {
		var model = this.GetEquipIndex(index);
		model.Id = id;
		model.Variant = variant;
		this.SetEquipIndex(index, model);
	}

	public void SetEquipStainId(EquipIndex index, byte stainId) {
		var model = this.GetEquipIndex(index);
		model.Stain0 = stainId;
		this.SetEquipIndex(index, model);
	}
	
	public void SetEquipStainId2(EquipIndex index, byte stainId) {
		var model = this.GetEquipIndex(index);
		model.Stain1 = stainId;
		this.SetEquipIndex(index, model);
	}

	private unsafe void ForceUpdateEquipIndex(EquipIndex index) {
		if (!actor.IsValid) return;
		
		var chara = actor.GetCharacter();
		if (chara == null) return;
		
		var current = this.GetEquipIndex(index);
		chara->FlagSlotForUpdate((uint)index, &current);
	}
	
	// Hat visibility

	public unsafe bool GetHatVisible() => actor.IsValid
		&& actor.Character != null
		&& actor.Appearance.CheckHatVisible(!actor.Character->DrawData.IsHatHidden);

	public unsafe void SetHatVisible(bool visible) {
		if (!actor.IsValid || actor.Character == null) return;
		this.SetStateIfNotTracked(EquipIndex.Head);
		actor.Appearance.HatVisible = visible ? EquipmentToggle.On : EquipmentToggle.Off;
		actor.Character->DrawData.HideHeadgear(0, !visible);
		if (visible) this.ForceUpdateEquipIndex(EquipIndex.Head);
	}
	
	// Visor toggle

	public unsafe bool GetVisorToggled() => actor.IsValid
		&& actor.Character != null
		&& actor.Appearance.CheckVisorToggled(actor.Character->DrawData.IsVisorToggled);

	public unsafe void SetVisorToggled(bool toggled) {
		if (!actor.IsValid || actor.Character == null) return;
		actor.Appearance.VisorToggled = toggled ? EquipmentToggle.On : EquipmentToggle.Off;
		actor.Character->DrawData.SetVisor(toggled);
	}
	
	// Weapon wrappers

	public unsafe WeaponModelId GetWeaponIndex(WeaponIndex index) {
		if (!actor.IsValid) return default;
		if (actor.Appearance.Weapons.IsSet(index))
			return actor.Appearance.Weapons[index];
		var data = GetWeaponData(actor, index);
		return data != null ? data->ModelId : default;
	}

	public unsafe void SetWeaponIndex(WeaponIndex index, WeaponModelId model) {
		if (!actor.IsValid) return;
		actor.Appearance.Weapons[index] = model;
		var chara = actor.Character;
		if (chara != null)
			chara->DrawData.LoadWeapon((DrawDataContainer.WeaponSlot)index, model, 0, 0, 0, 0);
	}

	public void SetWeaponIdBaseVariant(WeaponIndex index, ushort id, ushort second, byte variant) {
		var model = this.GetWeaponIndex(index);
		model.Id = id;
		model.Type = second;
		model.Variant = variant;
		this.SetWeaponIndex(index, model);
	}

	public void SetWeaponStainId(WeaponIndex index, byte stainId) {
		var model = this.GetWeaponIndex(index);
		model.Stain0 = stainId;
		this.SetWeaponIndex(index, model);
	}
	
	public void SetWeaponStainId2(WeaponIndex index, byte stainId) {
		var model = this.GetWeaponIndex(index);
		model.Stain1 = stainId;
		this.SetWeaponIndex(index, model);
	}
	
	private unsafe static DrawObjectData* GetWeaponData(ActorEntity actor, WeaponIndex index) {
		if (!actor.IsValid || actor.Character == null) return null;
		fixed (DrawObjectData* ptr = &actor.Character->DrawData.WeaponData[(int)index])
			return ptr;
	}
	
	// Weapon visible

	public unsafe bool GetWeaponVisible(WeaponIndex index) {
		var data = GetWeaponData(actor, index);
		return data != null && actor.Appearance.Weapons.CheckVisible(index, !data->IsHidden);
	}

	public unsafe void SetWeaponVisible(WeaponIndex index, bool visible) {
		actor.Appearance.Weapons.SetVisible(index, visible);
		var data = GetWeaponData(actor, index);
		if (data != null) data->IsHidden = !visible;
	}

	private void UpdateWeaponVisibleState(WeaponIndex index) {
		var state = actor.Appearance.Weapons.GetVisible(index);
		if (state != EquipmentToggle.None)
			this.SetWeaponVisible(index, state == EquipmentToggle.On);
	}
	
	// Set GameObject state (for spawned actors)

	public unsafe void ApplyStateToGameObject() {
		if (!actor.IsValid || actor.Character == null) return;
		foreach (var value in Enum.GetValues<EquipIndex>()) {
			var model = this.GetEquipIndex(value);
			actor.Character->DrawData.LoadEquipment((DrawDataContainer.EquipmentSlot)value, &model, true);
		}
	}
}
