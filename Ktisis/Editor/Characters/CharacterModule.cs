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
	
	[Signature("E8 ?? ?? ?? ?? 4C 8B BC 24 ?? ?? ?? ?? 4C 8D 9C 24 ?? ?? ?? ?? 49 8B 5B 40", DetourName = nameof(SetTimelineIdDetour))]
	public Hook<SetTimelineIdDelegate> SetTimelineId = null!;
	public unsafe delegate bool SetTimelineIdDelegate(AnimationTimeline* a1, ushort a2, nint a3);
	public unsafe bool SetTimelineIdDetour(AnimationTimeline* a1, ushort a2, nint a3) {
		if (((CharacterEx*)((nint)a1 - 0x9B0 - 0x10))->Character.ObjectIndex == 201) {
			//if (a2 == 3) return false;
		}
		var exec = this.SetTimelineId.Original(a1, a2, a3);
		//if (((CharacterEx*)((nint)a1 - 0x9B0 - 0x10))->Character.ObjectIndex == 201) {
			Ktisis.Log.Info($"TId {(nint)a1:X} {a2:X} {a3:X} => {exec}");
		//}
		return exec;
	}

	[Signature("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 44 38 75 7F", DetourName = nameof(SchedulerActionDetour))]
	private Hook<SchedulerActionDelegate> SchedulerAction = null!;
	private unsafe delegate nint SchedulerActionDelegate(nint a1, ushort a2, uint a3, nint a4, nint a5);
	private unsafe nint SchedulerActionDetour(nint a1, ushort a2, uint a3, nint a4, nint a5) {
		var exec = this.SchedulerAction.Original(a1, a2, a3, a4, a5);
		if (a2 != 3) Ktisis.Log.Info($"Act {a1:X} {a2:X} {a3:X} {a4:X} {a5:X} => {exec:X}");
		return exec;
	}

	[Signature("E8 ?? ?? ?? ?? 88 45 68", DetourName = nameof(UnkEmote0Detour))]
	private Hook<UnkEmote0> UnkEmote0Hook = null!;
	private unsafe delegate bool UnkEmote0(nint a1, nint a2, nint a3, nint a4);
	private unsafe bool UnkEmote0Detour(nint a1, nint a2, nint a3, nint a4) {
		if (a2 == 0xF) a2 = 126;
		var exec = this.UnkEmote0Hook.Original(a1, a2, a3, a4);
		Ktisis.Log.Info($"{a1:X} {a2:X} {a3:X} {a4:X} => {exec}");
		return exec;
	}
}
