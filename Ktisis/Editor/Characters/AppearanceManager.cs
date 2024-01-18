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
	public void SetEquipIndexIdVariant(ActorEntity actor, EquipIndex index, ushort id, byte variant);
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
		return actor.CharacterEx != null ? actor.CharacterEx->Equipment[(uint)index] : default;
	}
	
	public unsafe void SetEquipIndex(ActorEntity actor, EquipIndex index, EquipmentModelId model) {
		if (!actor.IsValid) return;
		actor.Appearance.Equipment[index] = model;
		var chara = actor.GetCharacter();
		if (chara != null) chara->FlagSlotForUpdate((uint)index, &model);
	}

	public void SetEquipIndexIdVariant(ActorEntity actor, EquipIndex index, ushort id, byte variant) {
		var model = this.GetEquipIndex(actor, index);
		model.Id = id;
		model.Variant = variant;
		this.SetEquipIndex(actor, index, model);
	}
	
	// Disposal

	public void Dispose() {
		this.Module?.Dispose();
		this.Module = null;
		GC.SuppressFinalize(this);
	}
}
