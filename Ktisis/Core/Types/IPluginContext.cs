using Ktisis.Actions;
using Ktisis.Data.Config;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface;
using Ktisis.Interop.Ipc;
using Ktisis.Services.Plugin;

namespace Ktisis.Core.Types;

public interface IPluginContext {
	public ActionsService Actions { get; }
	public ConfigManager Config { get; }
	public GuiManager Gui { get; }
	public IpcManager Ipc { get; }
	
	public IEditorContext? Editor { get; }
}
