using System;
using System.Linq;

using Dalamud.Game.ClientState.Objects.Types;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using Ktisis.Editor.Characters.Data;
using Ktisis.Editor.Context;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters;

public interface IAppearanceManager : IDisposable {
	public bool IsValid { get; }
	
	public void Initialize();

	public bool TryGetStateForActor(GameObject actor, out ActorEntity entity, out AppearanceState state);
	
	public void ApplyStateFlagsFor(ActorEntity actor);

	public EquipmentModelId GetEquipIndex(ActorEntity actor, EquipIndex index);
	public void SetEquipIndex(ActorEntity actor, EquipIndex index, EquipmentModelId model);
	public void SetEquipIdVariant(ActorEntity actor, EquipIndex index, ushort id, byte variant);
	public void SetEquipStainId(ActorEntity actor, EquipIndex index, byte stainId);

	public bool GetHatVisible(ActorEntity actor);
	public void SetHatVisible(ActorEntity actor, bool visible);

	public WeaponModelId GetWeaponIndex(ActorEntity actor, WeaponIndex index);
	public void SetWeaponIndex(ActorEntity actor, WeaponIndex index, WeaponModelId model);
	public void SetWeaponIdBaseVariant(ActorEntity actor, WeaponIndex index, ushort id, ushort second, byte variant);
	public void SetWeaponStainId(ActorEntity actor, WeaponIndex index, byte stainId);

	public bool GetWeaponVisible(ActorEntity actor, WeaponIndex index);
	public void SetWeaponVisible(ActorEntity actor, WeaponIndex index, bool visible);

	public bool GetEquipmentFlag(ActorEntity actor, EquipmentFlags flag);
	public void SetEquipmentFlag(ActorEntity actor, EquipmentFlags flag, bool value);
}

public class AppearanceManager : IAppearanceManager {
	private readonly IContextMediator _mediator;
	private readonly HookScope _scope;

	public bool IsValid => this._mediator.Context.IsValid;
    
	public AppearanceManager(
		IContextMediator mediator,
		HookScope scope
	) {
		this._mediator = mediator;
		this._scope = scope;
	}
	
	// Initialization
	
	private AppearanceModule? Module { get; set; }

	public void Initialize() {
		Ktisis.Log.Verbose("Initializing appearance module...");
		try {
			this.Module = this._scope.Create<AppearanceModule>(this);
			this.Module.Initialize();
			this.Module.EnableAll();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize appearance editor:\n{err}");
		}
	}
	
	// State wrappers

	public bool TryGetStateForActor(GameObject actor, out ActorEntity entity, out AppearanceState state) {
		var _entity = this.GetEntityForActor(actor);
		entity = _entity!;
		state = _entity?.Appearance!;
		return _entity != null;
	}

	public void ApplyStateFlagsFor(ActorEntity entity) {
		this.UpdateWeaponVisibleState(entity, WeaponIndex.MainHand);
		this.UpdateWeaponVisibleState(entity, WeaponIndex.OffHand);
	}
	
	private void SetStateIfNotTracked(ActorEntity actor, EquipIndex index) {
		if (!actor.IsValid || actor.Appearance.Equipment.IsSet(index)) return;
		actor.Appearance.Equipment[index] = this.GetEquipIndex(actor, index);
	}
	
	private ActorEntity? GetEntityForActor(GameObject actor) => this._mediator.Context.Scene.Children
		.Where(entity => entity is ActorEntity { IsValid: true })
		.Cast<ActorEntity>()
		.FirstOrDefault(entity => entity.Actor.Equals(actor));
	
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

	public unsafe bool GetHatVisible(ActorEntity actor) => actor.IsValid && actor.Character != null
		&& actor.Appearance.Equipment.CheckHatVisible(!actor.Character->DrawData.IsHatHidden);

	public unsafe void SetHatVisible(ActorEntity actor, bool visible) {
		if (!actor.IsValid || actor.Character == null) return;
		this.SetStateIfNotTracked(actor, EquipIndex.Head);
		actor.Appearance.Equipment.HatVisible = visible ? EquipmentVisible.Visible : EquipmentVisible.Hidden;
		actor.Character->DrawData.HideHeadgear(0, !visible);
		if (visible) this.ForceUpdateEquipIndex(actor, EquipIndex.Head);
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
		if (state != EquipmentVisible.None)
			this.SetWeaponVisible(actor, index, state == EquipmentVisible.Visible);
	}
	
	// Equipment flags

	public unsafe bool GetEquipmentFlag(ActorEntity actor, EquipmentFlags flag) {
		if (!actor.IsValid || actor.CharacterBaseEx == null) return false;
		switch (flag) {
			case EquipmentFlags.SetVisor:
				return actor.CharacterBaseEx->Base.VisorToggled;
			default:
				return false;
		}
	}

	public unsafe void SetEquipmentFlag(ActorEntity actor, EquipmentFlags flag, bool value) {
		if (!actor.IsValid || actor.CharacterBaseEx == null) return;
		var state = actor.Appearance.Equipment;
		state.SetFlagState(flag, value);
		switch (flag) {
			case EquipmentFlags.SetVisor:
				actor.CharacterBaseEx->Base.VisorToggled = value;
				break;
			default:
				break;
		}
	}
	
	// Disposal

	public void Dispose() {
		this.Module?.Dispose();
		this.Module = null;
		GC.SuppressFinalize(this);
	}
}
