using System;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using Ktisis.Interop.Hooking;
using Ktisis.Services;

using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Ktisis.Scene.Modules;

public class ActorModule : SceneModule {
	private readonly ActorService _actors;
	
	public ActorModule(
		IHookMediator hook,
		ISceneManager scene,
		ActorService actors
	) : base(hook, scene) {
		this._actors = actors;
	}

	public override void Setup() {
		foreach (var actor in this._actors.GetGPoseActors())
			this.AddActor(actor, false);
		this.EnableAll();

		Ktisis.Log.Info($"InitActorHook: {this.AddCharacterHook != null}");
	}
	
	// Hooks

	private const ulong InvalidId = 0xE0000000;
	
	[Signature("E8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 BE ?? ?? ?? ?? ??", DetourName = nameof(AddCharacterDetour))]
	private Hook<AddCharacterDelegate>? AddCharacterHook = null!;
	private delegate void AddCharacterDelegate(nint a1, nint a2, ulong a3);

	private void AddCharacterDetour(nint gpose, nint address, ulong id) {
		this.AddCharacterHook!.Original(gpose, address, id);
		if (!this.CheckValid()) return;
		
		try {
			Ktisis.Log.Debug($"AddCharacter: {gpose:X} {address:X} {id:X}");
			if (id != InvalidId)
				this.AddActor(address, true);
			else
				Ktisis.Log.Verbose($"Invalid ID for 0x{address:X}, ignoring.");
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to handle character add for 0x{address:X}:\n{err}");
		}
	}

	private void AddActor(nint address, bool addCompanion) {
		var actor = this._actors.GetAddress(address);
		if (actor != null)
			this.AddActor(actor, addCompanion);
		else
			Ktisis.Log.Warning($"Actor address at 0x{address:X} returned null.");
	}

	private void AddActor(GameObject actor, bool addCompanion) {
		if (actor.IsValid()) {
			this.Scene.Factory.CreateActor(actor).Add();
			if (addCompanion)
				this.AddCompanion(actor);
		} else {
			Ktisis.Log.Warning($"Actor address at 0x{actor.Address:X} is invalid!");
		}
	}

	private unsafe void AddCompanion(GameObject owner) {
		var chara = (Character*)owner.Address;
		if (chara == null || chara->CompanionObject == null) return;
		
		var actor = this._actors.GetAddress((nint)chara->CompanionObject);
		if (actor is null or { ObjectIndex: 0 } || !actor.IsValid()) return;
		
		this.Scene.Factory.CreateActor(actor).Add();
	}
}
