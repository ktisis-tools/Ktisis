using System;

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Services;
using Ktisis.Interop.Hooking;
using Ktisis.Editor.Characters.Data;

namespace Ktisis.Editor.Characters;

public class AppearanceModule : HookModule {
	private readonly IAppearanceManager Manager;

	private readonly ActorService _actors;

	private bool IsValid => this.Manager.IsValid;

	public AppearanceModule(
		IHookMediator hook,
		IAppearanceManager manager,
		ActorService actors
	) : base(hook) {
		this.Manager = manager;
		this._actors = actors;
	}
	
	// Hooks

	[Signature("E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9 74 33 45 33 C0", DetourName = nameof(EnableDrawDetour))]
	private Hook<EnableDrawDelegate> EnableDrawHook = null!;
	private unsafe delegate nint EnableDrawDelegate(GameObject* gameObject);
	private unsafe nint EnableDrawDetour(GameObject* gameObject) {
		if (!this.IsValid) return this.EnableDrawHook.Original(gameObject);

		var c1 = ((byte)gameObject->TargetableStatus & 0x80) != 0;
		var c2 = (gameObject->RenderFlags & 0x2000000) == 0;
		var isNew = !(c1 && c2);
		
		var result = this.EnableDrawHook.Original(gameObject);
		if (!isNew) return result;

		try {
			this.UpdateCharacter(gameObject);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to handle character update:\n{err}");
		}
		
		return result;
	}

	private unsafe void UpdateCharacter(GameObject* gameObject) {
		if (gameObject == null || gameObject->DrawObject == null) return;
		
		if (gameObject->DrawObject->Object.GetObjectType() != ObjectType.CharacterBase) return;

		var actor = this._actors.GetAddress((nint)gameObject);
		if (actor == null) return;

		var state = this.Manager.GetStateForActor(actor);
		if (state != null) this.ApplyState((CharacterBase*)gameObject->DrawObject, state);
	}

	private unsafe void ApplyState(CharacterBase* chara, AppearanceState state) {
		if (chara->GetModelType() != CharacterBase.ModelType.Human) return;

		// TODO: Apply customize
		
		// Apply equipment
		
		foreach (var index in Enum.GetValues<EquipIndex>()) {
			if (!state.Equipment.IsSet(index)) continue;
			var model = state.Equipment[index];
			chara->FlagSlotForUpdate((uint)index, &model);
		}
	}
}
