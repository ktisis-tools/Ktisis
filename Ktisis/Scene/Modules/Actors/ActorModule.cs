using System;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

using Ktisis.Structs.GPose;
using Ktisis.Structs.Actors;
using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;
using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Scene.Types;
using Ktisis.Services.Game;

namespace Ktisis.Scene.Modules.Actors;

public class ActorModule : SceneModule {
	private readonly ActorService _actors;
	private readonly IClientState _clientState;
	private readonly IFramework _framework;
	private readonly GroupPoseModule _gpose;
	
	private readonly ActorSpawner _spawner;
	
	public ActorModule(
		IHookMediator hook,
		ISceneManager scene,
		ActorService actors,
		IClientState clientState,
		IFramework framework,
		GroupPoseModule gpose
	) : base(hook, scene) {
		this._actors = actors;
		this._clientState = clientState;
		this._framework = framework;
		this._gpose = gpose;
		this._spawner = hook.Create<ActorSpawner>();
	}

	public override void Setup() {
		foreach (var actor in this._actors.GetGPoseActors())
			this.AddActor(actor, false);
		
		this.Subscribe();
		
		this.EnableAll();
		this._spawner.TryInitialize();
	}
	
	// Events

	private unsafe void Subscribe() {
		this.Scene.Context.Characters.OnDisableDraw += this.OnDisableDraw;
	}

	private unsafe void OnDisableDraw(IGameObject gameObject, DrawObject* drawObject) {
		if (!this.IsInit || !this.Scene.IsValid) return;

		var entity = this.Scene.GetEntityForActor(gameObject);
		if (entity == null) return;
		
		entity.Address = nint.Zero;
		
		Ktisis.Log.Debug($"Invalidated object address for entity '{entity.Name}' ({gameObject.ObjectIndex})");
	}
	
	// Spawning

	public async Task<ActorEntity> Spawn() {
		var localPlayer = this._clientState.LocalPlayer;
		if (localPlayer == null)
			throw new Exception("Local player not found.");
		
		var address = await this._spawner.CreateActor(localPlayer);
		var entity = this.AddSpawnedActor(address);
		entity.Actor.SetName(PlayerNameUtil.CalcActorName(entity.Actor.ObjectIndex));
		entity.Actor.SetWorld((ushort)localPlayer.CurrentWorld.RowId);
		this.ReassignParentIndex(entity.Actor);
		return entity;
	}

	public async Task<ActorEntity> AddFromOverworld(IGameObject actor) {
		if (!this._spawner.IsInit)
			throw new Exception("Actor spawner is uninitialized.");
		var address = await this._spawner.CreateActor(actor);
		var entity = this.AddSpawnedActor(address);
		entity.Actor.SetTargetable(true);
		return entity;
	}

	private ActorEntity AddSpawnedActor(nint address) {
		var entity = this.AddActor(address, false);
		if (entity == null)
			throw new Exception("Failed to create entity for spawned actor.");
		entity.IsManaged = true;
		return entity;
	}
	
	// Deletion
	
	public unsafe void Delete(ActorEntity actor) {
		if (this._gpose.IsPrimaryActor(actor)) {
			Ktisis.Log.Warning("Refusing to delete primary actor.");
			return;
		}
		
		var gpose = this._gpose.GetGPoseState();
		if (gpose == null) return;

		var gameObject = (CSGameObject*)actor.Actor.Address;
		this._framework.RunOnFrameworkThread(() => {
			var mgr = ClientObjectManager.Instance();
			var index = (ushort)mgr->GetIndexByObject(gameObject);
			this._removeCharacter(gpose, gameObject);
			if (index != ushort.MaxValue)
				mgr->DeleteObjectByIndex(index, 1);
		});
		
		actor.Remove();
	}
	
	// Spawned actor state

	private void ReassignParentIndex(IGameObject gameObject) {
		var ipcMgr = this.Scene.Context.Plugin.Ipc;
		if (ipcMgr.IsPenumbraActive) {
			var penumbra = ipcMgr.GetPenumbraIpc();
			penumbra.SetAssignedParentIndex(gameObject, gameObject.ObjectIndex);
		}
		if (ipcMgr.IsCustomizeActive) {
			var customize = ipcMgr.GetCustomizeIpc();
			
			if (customize.IsCompatible())
				customize.SetCutsceneParentIndex(gameObject.ObjectIndex, gameObject.ObjectIndex);
		}
	}
	
	// Entities

	private ActorEntity? AddActor(nint address, bool addCompanion) {
		var actor = this._actors.GetAddress(address);
		if (actor is { ObjectIndex: not 200 })
			return this.AddActor(actor, addCompanion);
		Ktisis.Log.Warning($"Actor address at 0x{address:X} is invalid.");
		return null;
	}

	private ActorEntity? AddActor(IGameObject actor, bool addCompanion) {
		if (!actor.IsValid()) {
			Ktisis.Log.Warning($"Actor address at 0x{actor.Address:X} is invalid.");
			return null;
		}
		
		var result = this.Scene.Factory.BuildActor(actor).Add();
		if (addCompanion)
			this.AddCompanion(actor);
		return result;
	}

	private unsafe void AddCompanion(IGameObject owner) {
		var chara = (Character*)owner.Address;
		if (chara == null || chara->CompanionObject == null) return;
		
		var actor = this._actors.GetAddress((nint)chara->CompanionObject);
		if (actor is null or { ObjectIndex: 0 } || !actor.IsValid()) return;
		
		this.Scene.Factory.BuildActor(actor).Add();
	}
	
	public void RefreshGPoseActors() {
		var current = this.Scene.Children
			.Where(entity => entity is ActorEntity)
			.Cast<ActorEntity>()
			.ToList();

		foreach (var actor in current) {
			if (!actor.IsValid) continue;

			var entityForActor = this.Scene.GetEntityForActor(actor.Actor);
			if (entityForActor == null) continue;

			unsafe {
				if (entityForActor.Character != null) continue;
			}

			this.Delete(entityForActor);
		}

		foreach (var actor in this._actors.GetGPoseActors()) {
			if (this.Scene.GetEntityForActor(actor) is not null) continue;
			this.AddActor(actor, false);
		}
	}
	
	// Hooks
	
	[Signature("40 56 57 48 83 EC 38 48 89 5C 24 ??", DetourName = nameof(AddCharacterDetour))]
	private Hook<AddCharacterDelegate>? AddCharacterHook = null!;
	private delegate void AddCharacterDelegate(nint a1, nint a2, ulong a3, nint a4);

	private void AddCharacterDetour(nint gpose, nint address, ulong id, nint a4) {
		this.AddCharacterHook!.Original(gpose, address, id, a4);
		if (!this.CheckValid()) return;
		
		try {
			if (id != 0xE0000000)
				this.AddActor(address, true);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to handle character add for 0x{address:X}:\n{err}");
		}
	}

	[Signature("45 33 D2 4C 8D 81 ?? ?? ?? ?? 41 8B C2 4C 8B C9 49 3B 10")]
	private RemoveCharacterDelegate _removeCharacter = null!;
	private unsafe delegate nint RemoveCharacterDelegate(GPoseState* gpose, CSGameObject* gameObject);

	[Signature("E8 ?? ?? ?? ?? 48 83 C3 08 48 83 EF 01 75 CF", DetourName = nameof(ControlGazeDetour))]
	private Hook <ControlGazeDelegate>? ControlGazeHook = null!;
	private delegate void ControlGazeDelegate(nint a1);
	private unsafe void ControlGazeDetour(nint a1) {
		if (!this.CheckValid()) {
			// skip everything if the detour is invalid
			this.ControlGazeHook.Original(a1);
			return;
		}

		// check current scene ActorEntities
		var current = this.Scene.Children
			.Where(entity => entity is ActorEntity)
			.Cast<ActorEntity>()
			.ToList();

		foreach (ActorEntity actor in current) {
			// valid actor with a modified gaze to use
			if (!actor.IsValid || actor.Gaze == null) continue;

			// actor address matches character being detoured
			if (actor.Actor.Address != a1 - CharacterEx.GazeOffset) continue;

			// get a characterEx we can work with from the gaze being detoured
			// overwrite gaze at a1 with stored gaze
			var detourCharacterEx = (CharacterEx*)(a1 - CharacterEx.GazeOffset);
			// detourCharacterEx->Gaze = actor.Gaze!;
			detourCharacterEx->Gaze = (ActorGaze)actor.Gaze;
		}

		this.ControlGazeHook.Original(a1);
	}

	
	// Disposal

	public override void Dispose() {
		base.Dispose();
		this._spawner.Dispose();
		GC.SuppressFinalize(this);
	}
}
