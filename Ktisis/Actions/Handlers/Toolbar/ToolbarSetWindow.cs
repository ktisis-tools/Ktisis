using Dalamud.Bindings.ImGuizmo;
using Dalamud.Game.ClientState.Keys;

using FFXIVClientStructs;

using Ktisis.Actions.Attributes;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;
using Ktisis.Interface.Windows;

namespace Ktisis.Actions.Handlers.Toolbar;

public abstract class ToolbarSetWindow (IPluginContext ctx) : KeyAction(ctx) {

	public override bool Invoke() {
		if (this.Context.Editor == null || !this.Context.Editor.Config.Editor.UseToolbar)
			return false;
		var window = this.Context.Gui.GetOrCreate<ToolbarWindow>(this.Context.Editor, this.Context.Gui);
		this.Call(window);
		return true;
	}

	internal abstract void Call(ToolbarWindow window);
}

[Action("Toolbar_ToggleWorkspace")]
public class ToggleWorkspace(IPluginContext ctx) : ToolbarSetWindow(ctx) {
	
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.KEY_1)
		}
	};

	internal override void Call(ToolbarWindow window) => window.DrawWorkspaceWindow();
	
}

[Action("Toolbar_ToggleObject")]
public class ToggleObject(IPluginContext ctx) : ToolbarSetWindow(ctx) {
	
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.KEY_2)
		}
	};

	override internal void Call(ToolbarWindow window) => window.DrawObjectWindow();
	
}

[Action("Toolbar_ToggleActor")]
public class ToggleActor(IPluginContext ctx) : ToolbarSetWindow(ctx) {
	
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.KEY_3)
		}
	};

	override internal void Call(ToolbarWindow window) => window.DrawActorWindow();
	
}

[Action("Toolbar_TogglePose")]
public class TogglePose(IPluginContext ctx) : ToolbarSetWindow(ctx) {
	
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.KEY_4)
		}
	};

	override internal void Call(ToolbarWindow window) => window.DrawPosingWindow();
	
}
[Action("Toolbar_ToggleEnv")]
public class ToggleEnv(IPluginContext ctx) : ToolbarSetWindow(ctx) {
	
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.KEY_5)
		}
	};

	override internal void Call(ToolbarWindow window) => window.DrawEnvWindow();
	
}
[Action("Toolbar_ToggleCamera")]
public class ToggleCamera(IPluginContext ctx) : ToolbarSetWindow(ctx) {
	
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.KEY_6)
		}
	};

	override internal void Call(ToolbarWindow window) => window.DrawCameraWindow();
	
}
[Action("Toolbar_ToggleConfig")]
public class ToggleConfig(IPluginContext ctx) : ToolbarSetWindow(ctx) {
	
	
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.KEY_7)
		}
	};

	override internal void Call(ToolbarWindow window) => window.DrawConfigWindow();

}
