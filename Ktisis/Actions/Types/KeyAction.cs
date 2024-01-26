using Ktisis.Actions.Binds;
using Ktisis.Core.Types;
using Ktisis.Data.Config.Actions;

namespace Ktisis.Actions.Types;

public abstract class KeyAction(IPluginContext ctx) : ActionBase(ctx), IKeybind {
	public abstract KeybindInfo BindInfo { get; }

	public ActionKeybind GetKeybind() {
		return this.Context.Config.File.Keybinds.GetOrSetDefault(
			this.GetName(),
			this.BindInfo.Default
		);
	}
}
