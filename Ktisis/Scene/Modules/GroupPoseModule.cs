using Dalamud.Utility.Signatures;
using Dalamud.Hooking;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game.Control;

using Ktisis.Editor.Context.Types;
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
