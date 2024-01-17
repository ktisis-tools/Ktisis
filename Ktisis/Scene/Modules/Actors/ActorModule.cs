using System;
using System.Threading.Tasks;

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Dalamud.Plugin.Services;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using CSGameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;
using Ktisis.Services;
using Ktisis.Structs.GPose;

namespace Ktisis.Scene.Modules.Actors;

public class ActorModule : SceneModule {
	private readonly ActorService _actors;
	private readonly GroupPoseModule _gpose;
	private readonly IFramework _framework;
	private readonly ActorSpawner _spawner;
	
	public ActorModule(
		IHookMediator hook,
		ISceneManager scene,
		ActorService actors,
		IFramework framework,
		GroupPoseModule gpose
	) : base(hook, scene) {
		this._actors = actors;
		this._gpose = gpose;
		this._framework = framework;
		this._spawner = hook.Create<ActorSpawner>();
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

	[Signature("45 33 D2 4C 8D 81 ?? ?? ?? ?? 41 8B C2 4C 8B C9 49 3B 10")]
	private RemoveCharacterDelegate _removeCharacter = null!;
	private unsafe delegate nint RemoveCharacterDelegate(GPoseState* gpose, CSGameObject* gameObject);
	
	// Disposal

	public override void Dispose() {
		base.Dispose();
		this._spawner.Dispose();
		GC.SuppressFinalize(this);
	}
}
