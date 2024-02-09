using System;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

using Ktisis.Data.Files;
using Ktisis.Editor.Characters.Handlers;
using Ktisis.Editor.Characters.State;
using Ktisis.Editor.Characters.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters;

public class CharacterManager : ICharacterManager {
	private readonly IEditorContext _context;
	private readonly HookScope _scope;
	private readonly IFramework _framework;

	public bool IsValid => this._context.IsValid;
	
	public event DisableDrawHandler? OnDisableDraw;

	public CharacterManager(
		IEditorContext context,
		HookScope scope,
		IFramework framework
	) {
		this._context = context;
		this._scope = scope;
		this._framework = framework;
	}
	
	// Initialization
	
	private CharacterModule? Module { get; set; }

	public void Initialize() {
		Ktisis.Log.Verbose("Initializing appearance module...");
		try {
			this.Module = this._scope.Create<CharacterModule>(this);
			this.Subscribe();
			this.Module.Initialize();
			this.Module.EnableAll();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize appearance editor:\n{err}");
		}
	}

	private unsafe void Subscribe() {
		this.Module!.OnDisableDraw += (gameObj, drawObj) => this.OnDisableDraw?.Invoke(gameObj, drawObj);
	}
	
	// Editors

	public ICustomizeEditor GetCustomizeEditor(ActorEntity actor) => new CustomizeEditor(actor);
	public IEquipmentEditor GetEquipmentEditor(ActorEntity actor) => new EquipmentEditor(actor);
	
	// State wrappers

	public bool TryGetStateForActor(GameObject actor, out ActorEntity entity, out AppearanceState state) {
		var needle = this._context.Scene.GetEntityForActor(actor);
		entity = needle!;
		state = needle?.Appearance!;
		return needle != null;
	}

	public void ApplyStateToGameObject(ActorEntity entity) {
		this.GetCustomizeEditor(entity).ApplyStateToGameObject();
		this.GetEquipmentEditor(entity).ApplyStateToGameObject();
	}
	
	// Imports

	public Task ApplyCharaFile(ActorEntity actor, CharaFile file, SaveModes modes = SaveModes.All, bool gameState = false) {
		var loader = new EntityCharaConverter(actor);
		return this._framework.RunOnFrameworkThread(() => {
			loader.Apply(file, modes);
			if (gameState)
				this.ApplyStateToGameObject(actor);
		});
	}

	public Task<CharaFile> SaveCharaFile(ActorEntity actor) {
		return this._framework.RunOnFrameworkThread(
			() => new EntityCharaConverter(actor).Save()
		);
	}
	
	// Disposal

	public void Dispose() {
		this.Module?.Dispose();
		this.Module = null;
		GC.SuppressFinalize(this);
	}
}
