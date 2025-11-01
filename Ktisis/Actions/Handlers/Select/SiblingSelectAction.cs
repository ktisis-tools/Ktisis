using System.Linq;

using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Attributes;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Editor.Selection;
using Ktisis.Editor.Transforms.Types;

namespace Ktisis.Actions.Handlers.Select;

[Action("Select_Sibling")]
public class SiblingSelectAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.OEM_5) // backslash
		}
	};

	public override bool CanInvoke() => this.Context.Editor?.Transform.Target?.Targets.Count() is 1;

	public override bool Invoke() {
        // bail if we have multiple or zero selections
		if (!this.CanInvoke()) return false;

		var target = this.Context.Editor!.Transform.Target!.Primary;
		if (target is not BoneNode bNode)
			return false;

		var siblingNode = bNode.Pose.TryResolveSibling(bNode);
		if (siblingNode == null)
			return false;

		this.Context.Editor.Selection.Select(siblingNode, SelectMode.Multiple);
		return true;
	}
}
