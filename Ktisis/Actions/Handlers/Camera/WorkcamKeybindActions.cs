using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Attributes;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;

namespace Ktisis.Actions.Handlers.Camera;

[Action("Camera_Work_Toggle")]
public class FreecamToggleAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.NO_KEY)
		}
	};

	public override bool CanInvoke() => this.Context.Editor != null;
	
	public override bool Invoke() {
		if (!this.CanInvoke()) return false;
		this.Context.Editor!.Cameras.ToggleWorkCameraMode();
		return true;
	}
}

// below actions are non-invoking stubs; purely here to redefine the keybinds checked by freecam's own input processing
[Action("Camera_Work_Back")]
public class WorkcamBackAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.S)
		}
	};
	
	public override bool Invoke() => false;
}

[Action("Camera_Work_Down")]
public class WorkcamDownAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.Q)
		}
	};
	
	public override bool Invoke() => false;
}

[Action("Camera_Work_Fast")]
public class WorkcamFastAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnHeld,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.SHIFT)
		}
	};
	
	public override bool Invoke() => false;
}

[Action("Camera_Work_Forward")]
public class WorkcamForwardAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.W)
		}
	};

	public override bool Invoke() => false;
}

[Action("Camera_Work_Left")]
public class WorkcamLeftAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.A)
		}
	};
	
	public override bool Invoke() => false;
}

[Action("Camera_Work_Right")]
public class WorkcamRightAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.D)
		}
	};
	
	public override bool Invoke() => false;
}

[Action("Camera_Work_Slow")]
public class WorkcamSlowAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnHeld,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.CONTROL)
		}
	};
	
	public override bool Invoke() => false;
}

[Action("Camera_Work_Up")]
public class WorkcamUpAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.SPACE)
		}
	};
	
	public override bool Invoke() => false;
}
