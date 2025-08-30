using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Attributes;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;

namespace Ktisis.Actions.Handlers.Camera;

[Action("Camera_Work_Left")]
public class WorkcamLeftAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.A)
		}
	};
	
    // stub action used purely for bindable referencing in WorkCamera.cs; should not override native inputs
	public override bool Invoke() => false;
}
