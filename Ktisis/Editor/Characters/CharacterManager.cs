using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

using Ktisis.Data.Files;
using Ktisis.Data.Mcdf;
using Ktisis.Editor.Characters.Handlers;
using Ktisis.Editor.Characters.State;
using Ktisis.Editor.Characters.Types;
using Ktisis.Editor.Context.Types;
using Ktisis.GameData.Excel.Types;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;

namespace Ktisis.Editor.Characters;

public class CharacterManager : ICharacterManager {
	private readonly IEditorContext _context;
	private readonly HookScope _scope;
	private readonly IFramework _framework;

	public bool IsValid => this._context.IsValid;
	
	public McdfManager Mcdf { get; }
	
	public event DisableDrawHandler? OnDisableDraw;

	public CharacterManager(
		IEditorContext context,
		HookScope scope,
		IFramework framework,
		McdfManager mcdf
	) {
		this._context = context;
		this._scope = scope;
		this._framework = framework;
		this.Mcdf = mcdf;
	}
	
	// Initialization
	
	private CharacterModule? Module { get; set; }
	
	public void Initialize() {
		Ktisis.Log.Verbose("Initializing character manager...");
		
		try {
			this.Module = this._scope.Create<CharacterModule>(this);
			this.Subscribe();
			this.Module.Initialize();
			this.Module.EnableAll();
			
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize character module:\n{err}");
		}
	}

	private unsafe void Subscribe() {
		this.Module!.OnDisableDraw += this.HandleDisableDraw;
		this.Module!.OnEnableDraw += this.HandleEnableDraw;
	}
	
	// Event handlers

	private readonly Dictionary<ushort, Transform> _savedTransforms = new();

	private unsafe void HandleDisableDraw(IGameObject gameObj, DrawObject* drawObj) {
		this.OnDisableDraw?.Invoke(gameObj, drawObj);
		if (drawObj != null)
			this._savedTransforms[gameObj.ObjectIndex] = *(Transform*)&drawObj->Position;
	}

	private unsafe void HandleEnableDraw(CSGameObject* gameObj) {
		var index = gameObj->ObjectIndex;
		if (!this._savedTransforms.TryGetValue(index, out var transform))
			return;

		if (gameObj->DrawObject != null)
			*(Transform*)&gameObj->DrawObject->Position = transform;

		this._savedTransforms.Remove(index);
	}
	
	// Editors

	public ICustomizeEditor GetCustomizeEditor(ActorEntity actor) => new CustomizeEditor(actor);
	public IEquipmentEditor GetEquipmentEditor(ActorEntity actor) => new EquipmentEditor(actor);
	
	private EntityCharaConverter BuildEntityConverter(ActorEntity actor) {
		var custom = this.GetCustomizeEditor(actor);
		var equip = this.GetEquipmentEditor(actor);
		return new EntityCharaConverter(actor, custom, equip);
	}
	
	// State wrappers

	public bool TryGetStateForActor(IGameObject actor, out ActorEntity entity, out AppearanceState state) {
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
		var loader = this.BuildEntityConverter(actor);
		return this._framework.RunOnFrameworkThread(() => {
			loader.Apply(file, modes);
			if (gameState) this.ApplyStateToGameObject(actor);
		});
	}

	public Task<CharaFile> SaveCharaFile(ActorEntity actor) {
		return this._framework.RunOnFrameworkThread(
			() => this.BuildEntityConverter(actor).Save()
		);
	}

	public Task ApplyNpc(ActorEntity actor, INpcBase npc, SaveModes modes = SaveModes.All, bool gameState = false) {
		var loader = this.BuildEntityConverter(actor);
		return this._framework.RunOnFrameworkThread(() => {
			loader.Apply(npc, modes);
			if (gameState) this.ApplyStateToGameObject(actor);
		});
	}
	
	// Disposal

	public void Dispose() {
		this.Module?.Dispose();
		this.Module = null;
		GC.SuppressFinalize(this);
	}
}
