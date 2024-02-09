using System;

using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using Dalamud.Game.ClientState.Objects.Enums;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Interop.Hooking;
using Ktisis.Editor.Characters.State;
using Ktisis.Editor.Characters.Types;
using Ktisis.Services.Data;
using Ktisis.Services.Game;
using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters;

public class CharacterModule : HookModule {
	private readonly ICharacterManager Manager;

	private readonly ActorService _actors;
	private readonly CustomizeService _discovery;

	private bool IsValid => this.Manager.IsValid;

	public event DisableDrawHandler? OnDisableDraw;

	public CharacterModule(
		IHookMediator hook,
		ICharacterManager manager,
		ActorService actors,
		CustomizeService discovery
	) : base(hook) {
		this.Manager = manager;
		this._actors = actors;
		this._discovery = discovery;
	}
	
	// Hooks

	private unsafe GameObject* _prepareCharaFor;
	
	// DisableDraw
	
	[Signature("E9 ?? ?? ?? ?? 48 83 C4 20 5B C3 CC CC CC 48 8B 41 48", DetourName = nameof(DisableDrawDetour))]
	private Hook<DisableDrawDelegate> DisableDrawHook = null!;
	private unsafe delegate nint DisableDrawDelegate(GameObject* chara);
	private unsafe nint DisableDrawDetour(GameObject* chara) {
		try {
			if (chara->DrawObject != null)
				this.HandleDisableDraw(chara);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to handle disable draw:\n{err}");
		}
		return this.DisableDrawHook.Original(chara);
	}

	private unsafe void HandleDisableDraw(GameObject* chara) {
		var actor = this._actors.GetAddress((nint)chara);
		if (actor != null)
			this.OnDisableDraw?.Invoke(actor, chara->DrawObject);
	}
	
	// EnableDraw

	[Signature("E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9 74 33 45 33 C0", DetourName = nameof(EnableDrawDetour))]
	private Hook<EnableDrawDelegate> EnableDrawHook = null!;
	private unsafe delegate nint EnableDrawDelegate(GameObject* gameObject);
	private unsafe nint EnableDrawDetour(GameObject* gameObject) {
		if (!this.IsValid) return this.EnableDrawHook.Original(gameObject);

		var c1 = ((byte)gameObject->TargetableStatus & 0x80) != 0;
		var c2 = (gameObject->RenderFlags & 0x2000000) == 0;
		var isNew = !(c1 && c2);

		if (!isNew) return this.EnableDrawHook.Original(gameObject);

		var result = nint.Zero;
		try {
			this._prepareCharaFor = gameObject;
			result = this.EnableDrawHook.Original(gameObject);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to handle character update:\n{err}");
		} finally {
			this._prepareCharaFor = null;
		}
		return result;
	}
	
	// CreateCharacter

	[Signature("E8 ?? ?? ?? ?? 48 8B 4E 08 48 8B D0 4C 8B 01", DetourName = nameof(CreateCharacterDetour))]
	private Hook<CreateCharacterDelegate> CreateCharacterHook = null!;
	private unsafe delegate CharacterBase* CreateCharacterDelegate(uint model, CustomizeContainer* customize, EquipmentContainer* equip, byte unk);
	private unsafe CharacterBase* CreateCharacterDetour(uint model, CustomizeContainer* customize, EquipmentContainer* equip, byte unk) {
		try {
			if (model == 0 && customize != null && equip != null)
				this.PreHandleCreate(customize, equip);
		} catch (Exception err) {
			Ktisis.Log.Info($"Failure on PreHandleCreate:\n{err}");
		}
		return this.CreateCharacterHook.Original(model, customize, equip, unk);
	}

	private unsafe void PreHandleCreate(CustomizeContainer* customize, EquipmentContainer* equip) {
		if (!this.IsValid || this._prepareCharaFor == null) return;

		var actor = this._actors.GetAddress((nint)this._prepareCharaFor);
		if (actor == null) return;

		if (!this.Manager.TryGetStateForActor(actor, out var entity, out var state))
			return;
		
		// Apply customize

		for (var i = 0; i < CustomizeContainer.Size; i++) {
			var index = (CustomizeIndex)i;
			if (!state.Customize.IsSet(index)) continue;
			customize->Bytes[i] = state.Customize[index];
		}
		
		// Validate face
		
		if (state.Customize.IsSet(CustomizeIndex.Tribe) || state.Customize.IsSet(CustomizeIndex.FaceType)) {
			var dataId = this._discovery.CalcDataIdFor(customize->Tribe, customize->Gender);
			var isValid = this._discovery.IsFaceIdValidFor(dataId, customize->FaceType);
			Ktisis.Log.Debug($"Face {customize->FaceType} for {dataId} is valid? {isValid}");
			if (!isValid) {
				var newId = this._discovery.FindBestFaceTypeFor(dataId, customize->FaceType);
				Ktisis.Log.Debug($"\tSetting {newId} as next best face type");
				state.Customize.SetIfActive(CustomizeIndex.FaceType, newId);
				customize->FaceType = newId;
			}
		}
		
		// Apply equipment

		for (uint i = 0; i < EquipmentContainer.Length; i++) {
			var index = (EquipIndex)i;
			
			// Check hat visibility.
			if (index == EquipIndex.Head && state.HatVisible == EquipmentToggle.Off) {
				*equip->GetData(i) = default;
				continue;
			}
			
			// Apply saved equipment state.
			if (!state.Equipment.IsSet(index)) continue;
			*equip->GetData(i) = state.Equipment[index];
		}
		
		this.Manager.GetEquipmentEditor(entity).ApplyStateFlags();
	}
}
