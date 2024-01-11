using System;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;
using Ktisis.Services;

using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Ktisis.Scene.Modules.Actors;

public class ActorModule : SceneModule {
	private readonly ActorService _actors;
	private readonly ActorSpawnManager _spawner;
	
	public ActorModule(
		IHookMediator hook,
		ISceneManager scene,
		ActorService actors,
		ActorSpawnManager spawner
	) : base(hook, scene) {
		this._actors = actors;
		this._spawner = spawner;
	}

	public override void Setup() {
		foreach (var actor in this._actors.GetGPoseActors())
			this.AddActor(actor, false);
		this.EnableAll();
		this._spawner.TryInitialize();
	}
	
	// Spawning

	public async Task<ActorEntity> Spawn() {
		if (!this._spawner.IsInit)
			throw new Exception("Actor spawn manager is uninitialized.");

		var address = await this._spawner.CreateActor();
		var entity = this.AddActor(address, false);
		if (entity == null) throw new Exception("Failed to create actor entity.");
		return entity;
	}
	
	// Entities

	private ActorEntity? AddActor(nint address, bool addCompanion) {
		var actor = this._actors.GetAddress(address);
		if (actor is { ObjectIndex: not 200 })
			return this.AddActor(actor, addCompanion);
		Ktisis.Log.Warning($"Actor address at 0x{address:X} is invalid.");
		return null;
	}

	private ActorEntity? AddActor(GameObject actor, bool addCompanion) {
		if (!actor.IsValid()) {
			Ktisis.Log.Warning($"Actor address at 0x{actor.Address:X} is invalid.");
			return null;
		}
		
		var result = this.Scene.Factory.CreateActor(actor).Add();
		if (addCompanion)
			this.AddCompanion(actor);
		return result;
	}

	private unsafe void AddCompanion(GameObject owner) {
		var chara = (Character*)owner.Address;
		if (chara == null || chara->CompanionObject == null) return;
		
		var actor = this._actors.GetAddress((nint)chara->CompanionObject);
		if (actor is null or { ObjectIndex: 0 } || !actor.IsValid()) return;
		
		this.Scene.Factory.CreateActor(actor).Add();
	}
	
	// Hooks
	
	[Signature("E8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 BE ?? ?? ?? ?? ??", DetourName = nameof(AddCharacterDetour))]
	private Hook<AddCharacterDelegate>? AddCharacterHook = null!;
	private delegate void AddCharacterDelegate(nint a1, nint a2, ulong a3);

	private void AddCharacterDetour(nint gpose, nint address, ulong id) {
		this.AddCharacterHook!.Original(gpose, address, id);
		if (!this.CheckValid()) return;
		
		try {
			if (id != 0xE0000000)
				this.AddActor(address, true);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to handle character add for 0x{address:X}:\n{err}");
		}
	}
	
	// Disposal

	public override void Dispose() {
		base.Dispose();
		this._spawner.Dispose();
	}
}
