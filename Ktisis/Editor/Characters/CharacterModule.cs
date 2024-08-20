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
using Ktisis.Structs.Actors;
using Ktisis.Structs.Characters;

namespace Ktisis.Editor.Characters;

public unsafe delegate void EnableDrawHandler(GameObject* gameObject);

public class CharacterModule : HookModule {
	private readonly ICharacterManager Manager;

	private readonly ActorService _actors;
	private readonly CustomizeService _discovery;

	private bool IsValid => this.Manager.IsValid;

	public event DisableDrawHandler? OnDisableDraw;
	public event EnableDrawHandler? OnEnableDraw;

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
	
	[Signature("40 53 48 83 EC 20 80 B9 ?? ?? ?? ?? ?? 48 8B D9 0F 8D ?? ?? ?? ??", DetourName = nameof(DisableDrawDetour))]
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
			this.OnEnableDraw?.Invoke(gameObject);
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
			if (customize != null && equip != null)
				this.PreHandleCreate(ref model, customize, equip);
		} catch (Exception err) {
			Ktisis.Log.Error($"Failure on PreHandleCreate:\n{err}");
		}
		return this.CreateCharacterHook.Original(model, customize, equip, unk);
	}

	private unsafe void PreHandleCreate(ref uint model, CustomizeContainer* customize, EquipmentContainer* equip) {
		if (!this.IsValid || this._prepareCharaFor == null) return;

		var actor = this._actors.GetAddress((nint)this._prepareCharaFor);
		if (actor == null) return;

		if (!this.Manager.TryGetStateForActor(actor, out var entity, out var state))
			return;
		
		// Model ID

		if (state.ModelId != null)
			model = state.ModelId.Value;
		
		// Apply customize

		for (var i = 0; i < CustomizeContainer.Size; i++) {
			var index = (CustomizeIndex)i;
			if (!state.Customize.IsSet(index)) continue;
			customize->Bytes[i] = state.Customize[index];
		}
		
		// Validate animation

		var chara = (CharacterEx*)this._prepareCharaFor;
		if (chara->Mode == 3 && chara->EmoteMode == 0)
			chara->Mode = 1;
		
		// Validate face
		
		if (state.Customize.IsSet(CustomizeIndex.Tribe) || state.Customize.IsSet(CustomizeIndex.FaceType)) {
			var dataId = this._discovery.CalcDataIdFor(customize->Tribe, customize->Gender);
			var isValid = this._discovery.IsFaceIdValidFor(dataId, customize->FaceType);

			Ktisis.Log.Debug($"Face {customize->FaceType} for {dataId} is valid? {isValid}");
			if (!isValid) {
				// highlander patch
				var newId = customize->FaceType;
				if (customize->Tribe == Tribe.Highlander && newId < 101) {
					newId += 100;
				} else {
					newId = this._discovery.FindBestFaceTypeFor(dataId, customize->FaceType);
				}
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
