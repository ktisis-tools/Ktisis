using System;
using System.Linq;

using Dalamud.Game.ClientState.Objects.Types;

using Ktisis.Editor.Characters.State;
using Ktisis.Editor.Characters.Handlers;
using Ktisis.Editor.Characters.Types;
using Ktisis.Editor.Context;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters;

public class AppearanceManager : IAppearanceManager {
	private readonly IContextMediator _mediator;
	private readonly HookScope _scope;

	public bool IsValid => this._mediator.Context.IsValid;

	public ICustomizeEditor Customize { get; } = new CustomizeEditor();
	public IEquipmentEditor Equipment { get; } = new EquipmentEditor();
    
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
	
	private ActorEntity? GetEntityForActor(GameObject actor) => this._mediator.Context.Scene.Children
		.Where(entity => entity is ActorEntity { IsValid: true })
		.Cast<ActorEntity>()
		.FirstOrDefault(entity => entity.Actor.ObjectIndex == actor.ObjectIndex);
	
	// Disposal

	public void Dispose() {
		this.Module?.Dispose();
		this.Module = null;
		GC.SuppressFinalize(this);
	}
}
