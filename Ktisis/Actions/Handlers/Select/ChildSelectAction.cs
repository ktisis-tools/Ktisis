using System.Linq;

using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Attributes;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;
using Ktisis.Scene.Entities.Skeleton;
using Ktisis.Editor.Selection;

namespace Ktisis.Actions.Handlers.Select;

[Action("Select_Children")]
public class ChildSelectAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.OEM_5, VirtualKey.CONTROL) // ctrl+backslash
		}
	};

	public override bool CanInvoke() => this.Context.Editor!.Selection.GetSelected().Count() == 1;

	public override bool Invoke() {
		// bail if we have multiple or zero selections
		if (!this.CanInvoke()) return false;

		// bail if selection has no children
		var selection = this.Context.Editor!.Selection.GetFirstSelected();
		if (selection == null || !selection.Children.Any())
			return false;

		// TODO: framerate hitch when selecting all bones
		foreach (var child in selection.Recurse()) child.Select(SelectMode.Multiple);
		return true;
	}
}
