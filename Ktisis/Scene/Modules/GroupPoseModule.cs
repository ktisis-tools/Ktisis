using Dalamud.Utility.Signatures;
using Dalamud.Hooking;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Types;
using Ktisis.Structs.GPose;
using Ktisis.Common.Extensions;

namespace Ktisis.Scene.Modules;

public class GroupPoseModule : SceneModule {
	private readonly IObjectTable _objectTable;

	public GroupPoseModule(
		IHookMediator hook,
		ISceneManager scene,
		IObjectTable objectTable
	) : base(hook, scene) {
		_objectTable = objectTable;
	}

	public override void Setup() {
		this.EnableAll();
	}

	// GPose state wrappers

	public unsafe GPoseState* GetGPoseState()
		=> this._getGPoseState != null ? this._getGPoseState() : null;
	
	// This is incorrect!
	/*public unsafe GameObject* GetPrimaryActor() {
		var gpose = this.GetGPoseState();
		return gpose != null ? gpose->GPoseTarget : null;
	}*/

	[Signature("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 41 56 48 83 EC ?? ?? ?? ?? 45 0F B6 F1", DetourName = nameof(SetCharacterTintDetour))]
	private Hook<SetCharacterTintDelegate> SetCharacterTintHook = null!;
	private unsafe delegate CharacterBase* SetCharacterTintDelegate(BattleChara* a1, nint a2, ushort a3, byte a4);

	private unsafe CharacterBase* SetCharacterTintDetour(BattleChara* a1, nint a2, ushort a3, byte a4) {
		if (this.Scene.Context.Config.Editor.DisableHighlighting)
			return a1->GetCharacterBase();
		else
			return this.SetCharacterTintHook.Original.Invoke(a1, a2, a3, a4);
	}
	
	public unsafe bool IsPrimaryActor(ActorEntity actor) {
		// TODO: This isn't accurate in cases where a lot of actors are fed into gpose!
		return actor.Actor.ObjectIndex is 200 or 201;
		// This is incorrect!
		/*var primary = this.GetPrimaryActor();
		return (nint)primary == actor.Actor.Address;*/
	}

	private unsafe IGameObject? GetGposeTarget() {
		var address = (nint)TargetSystem.Instance()->GPoseTarget;
		return this._objectTable.CreateObjectReference(address);
	}
	
	// Native

	[Signature("4C 8B DC 55 57 41 54 48 81 EC", DetourName = nameof(DetourToasts))]
	private Hook<ToastRefresh>? _refreshToastHook = null!;
	private unsafe delegate byte ToastRefresh(nint a1, uint a2, nint a3);

	private unsafe byte DetourToasts(nint a1, uint a2, nint a3) {
		// non-working - toasts still do not display when HideToasts is disabled
		return this.Scene.Context.Config.Editor.HideToasts ? (byte)1 : this._refreshToastHook!.Original(a1, a2, a3);
	}

	
	[Signature("E8 ?? ?? ?? ?? 0F B7 56 3C")]
	private GetGPoseStateDelegate? _getGPoseState = null;
	private unsafe delegate GPoseState* GetGPoseStateDelegate();

	[Signature("E8 ?? ?? ?? ?? 48 8D 8D ?? ?? ?? ?? 48 83 C4 28", DetourName = nameof(UpdateGposeTarNameDetour))]
	private Hook<UpdateGposeTarNameDelegate>? UpdateGposeTarNameHook = null;
	private unsafe delegate void UpdateGposeTarNameDelegate(nint a1);
	private unsafe void UpdateGposeTarNameDetour(nint a1) {
		if (!this.CheckValid()) {
			this.UpdateGposeTarNameHook!.Original(a1);
			return;
		}

		var targeted = this.GetGposeTarget();
		if (targeted != null && targeted.IsPcCharacter()) {
			var name = targeted.GetNameOrFallback(this.Scene.Context);
			for (var i = 0; i < name.Length; i++)
				*(char*)(a1 + 488 + i) = name[i];
		}

		this.UpdateGposeTarNameHook!.Original(a1);
	}
}
