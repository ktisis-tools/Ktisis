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

	public AppearanceState? GetStateForActor(GameObject actor);

	public EquipmentModelId GetEquipIndex(ActorEntity actor, EquipIndex index);
	public void SetEquipIndex(ActorEntity actor, EquipIndex index, EquipmentModelId model);
	public void SetEquipIdVariant(ActorEntity actor, EquipIndex index, ushort id, byte variant);
	public void SetEquipStainId(ActorEntity actor, EquipIndex index, byte stainId);

	public WeaponModelId GetWeaponIndex(ActorEntity actor, WeaponIndex index);
	public void SetWeaponIndex(ActorEntity actor, WeaponIndex index, WeaponModelId model);
	public void SetWeaponIdBaseVariant(ActorEntity actor, WeaponIndex index, ushort id, ushort second, byte variant);
	public void SetWeaponStainId(ActorEntity actor, WeaponIndex index, byte stainId);
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

	public AppearanceState? GetStateForActor(GameObject actor) {
		return this._mediator.Context.Scene.Children
			.Where(entity => entity is ActorEntity { IsValid: true })
			.Cast<ActorEntity>()
			.FirstOrDefault(entity => entity.Actor.Equals(actor))?
			.Appearance;
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
	
	// Weapon wrappers

	public unsafe WeaponModelId GetWeaponIndex(ActorEntity actor, WeaponIndex index) {
		if (!actor.IsValid || actor.Character == null) return default;
		if (actor.Appearance.Weapons.IsSet(index))
			return actor.Appearance.Weapons[index];
		return actor.Character->DrawData.WeaponDataSpan[(int)index].ModelId;
	}

	public unsafe void SetWeaponIndex(ActorEntity actor, WeaponIndex index, WeaponModelId model) {
		if (!actor.IsValid) return;
		actor.Appearance.Weapons[index] = model;
		var chara = actor.Character;
		if (chara != null) chara->DrawData.LoadWeapon((DrawDataContainer.WeaponSlot)index, model, 0, 0, 0, 0);
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
	
	// Equipment flags

	public unsafe bool GetEquipmentFlag(ActorEntity actor, EquipmentFlag flag) {
		if (!actor.IsValid || actor.CharacterBaseEx == null) return false;
		return flag switch {
			EquipmentFlag.SetVisor => actor.CharacterBaseEx->Base.VisorToggled,
			_ => false
		};
	}

	private unsafe void SetEquipmentFlag(ActorEntity actor, EquipmentFlag flag, bool value) {
		if (!actor.IsValid || actor.CharacterBaseEx == null) return;
		var state = actor.Appearance.Equipment;
		switch (flag) {
			case EquipmentFlag.SetVisor:
				state.SetFlagState(flag, value);
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
