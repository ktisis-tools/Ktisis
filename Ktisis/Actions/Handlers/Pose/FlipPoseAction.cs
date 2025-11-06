using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Attributes;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;
using Ktisis.Scene.Entities.Game;
using Ktisis.Scene.Entities.Skeleton;

namespace Ktisis.Actions.Handlers.Pose;

[Action("Pose_FlipPose")]
public class FlipPoseAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.F, VirtualKey.CONTROL)
		}
	};

	public override bool CanInvoke() {
		var target = this.Context.Editor?.Transform.Target?.Primary;
		return target != null && (target is ActorEntity || target is BoneNode);
	}

	public override bool Invoke() {
		if(!this.CanInvoke()) return false;

		var target = this.Context.Editor!.Transform.Target!.Primary;
		var pose = target is ActorEntity actor ? actor.Pose : target is BoneNode bNode ? bNode.Pose : null;
		if(pose != null) {
			this.Context.Editor.Posing.ApplyFlipPose(pose);
		}

		return true;
	}
}
