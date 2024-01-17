using Dalamud.Utility.Signatures;

using Ktisis.Interop.Hooking;
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
	
	// Native
	
	[Signature("E8 ?? ?? ?? ?? 0F B7 57 3C")]
	private GetGPoseStateDelegate? _getGPoseState = null;
	private unsafe delegate GPoseState* GetGPoseStateDelegate();
}
