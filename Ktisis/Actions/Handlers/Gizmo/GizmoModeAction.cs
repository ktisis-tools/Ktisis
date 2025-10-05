using Dalamud.Game.ClientState.Keys;

using Ktisis.Actions.Attributes;
using Ktisis.Actions.Binds;
using Ktisis.Actions.Types;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;
using Ktisis.ImGuizmo;
using Ktisis.Editor.Transforms;

namespace Ktisis.Actions.Handlers.Gizmo;

[Action("Gizmo_ToggleMode")]
public class GizmoModeAction(IPluginContext ctx) : KeyAction(ctx) {
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = true,
			Combo = new KeyCombo(VirtualKey.X, VirtualKey.CONTROL)
		}
	};
	
	public override bool Invoke() {
		if (this.Context.Editor == null || this.Context.Editor.Selection.Count == 0)
			return false;
		// ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
		this.Context.Config.File.Gizmo.Mode ^= Mode.World;
		return true;
	}
}

[Action("Gizmo_Parallel")]
public class ParallelAction(IPluginContext ctx) : GizmoModeAction(ctx)
{
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = false,
			Combo = new KeyCombo(VirtualKey.Z, VirtualKey.MENU),
		}
	};

	public override bool Invoke() {
		if (this.Context.Editor == null)
			return false;

		this.Context.Editor.Config.Gizmo.MirrorRotation = MirrorMode.Parallel;
		return true;
	}
}

[Action("Gizmo_Inverse")]
public class InverseAction(IPluginContext ctx) : GizmoModeAction(ctx)
{
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = false,
			Combo = new KeyCombo(VirtualKey.X, VirtualKey.MENU),
		}
	};

	public override bool Invoke() {
		if (this.Context.Editor == null)
			return false;

		this.Context.Editor.Config.Gizmo.MirrorRotation = MirrorMode.Inverse;
		return true;
	}
}

[Action("Gizmo_Reflect")]
public class ReflectAction(IPluginContext ctx) : GizmoModeAction(ctx)
{
	public override KeybindInfo BindInfo { get; } = new() {
		Trigger = KeybindTrigger.OnDown,
		Default = new ActionKeybind {
			Enabled = false,
			Combo = new KeyCombo(VirtualKey.C, VirtualKey.MENU),
		}
	};

	public override bool Invoke() {
		if (this.Context.Editor == null)
			return false;

		this.Context.Editor.Config.Gizmo.MirrorRotation = MirrorMode.Reflect;
		return true;
	}
}