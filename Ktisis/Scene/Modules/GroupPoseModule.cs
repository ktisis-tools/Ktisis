using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.Game.Object;

using Ktisis.Interop.Hooking;
using Ktisis.Scene.Entities.Game;
using Ktisis.Structs.GPose;

namespace Ktisis.Scene.Modules;

public class GroupPoseModule : SceneModule {
	public GroupPoseModule(
		IHookMediator hook,
		ISceneManager scene
	) : base(hook, scene) { }
	
	// GPose state wrappers

	public unsafe GPoseState* GetGPoseState()
		=> this._getGPoseState != null ? this._getGPoseState() : null;
	
	public unsafe GameObject* GetPrimaryActor() {
		var gpose = this.GetGPoseState();
		return gpose != null ? gpose->PrimaryActor : null;
	}

	public unsafe bool IsPrimaryActor(ActorEntity actor) {
		var primary = this.GetPrimaryActor();
		return (nint)primary == actor.Actor.Address;
	}
	
	// Native
	
	[Signature("E8 ?? ?? ?? ?? 0F B7 57 3C")]
	private GetGPoseStateDelegate? _getGPoseState = null;
	private unsafe delegate GPoseState* GetGPoseStateDelegate();
}
