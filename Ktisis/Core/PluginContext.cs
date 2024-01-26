using Ktisis.Actions;
using Ktisis.Core.Attributes;
using Ktisis.Core.Types;
using Ktisis.Data.Config;
using Ktisis.Editor.Context;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface;
using Ktisis.Interop;
using Ktisis.Interop.Ipc;
using Ktisis.Legacy;
using Ktisis.Services.Plugin;

namespace Ktisis.Core;

[Singleton]
public class PluginContext : IPluginContext {
	private readonly CommandService _cmd;
	private readonly DllResolver _dll;
	private readonly ContextManager _context;
	private readonly LegacyMigrator _legacy;
	
	public ActionsService Actions { get; }
	public ConfigManager Config { get; }
	public GuiManager Gui { get; }
	public IpcManager Ipc { get; }

	public IEditorContext? Editor => this._context.Current;
	
	public PluginContext(
		ActionsService actions,
		ConfigManager cfg,
		CommandService cmd,
		DllResolver dll,
		ContextManager context,
		GuiManager gui,
		IpcManager ipc,
		LegacyMigrator legacy
	) {
		this._cmd = cmd;
		this._dll = dll;
		this._context = context;
		this._legacy = legacy;
		
		this.Actions = actions;
		this.Config = cfg;
		this.Gui = gui;
		this.Ipc = ipc;
	}

	public void Initialize() {
		if (this.Config.GetConfigFileExists())
			this.Setup();
		else
			this.SetupLegacy();
		this.Gui.Initialize();
	}

	private void Setup() {
		this.Config.Load();
		this._dll.Create();
		this.Actions.RegisterActions(this);
		this._context.Initialize(this);
		this._cmd.RegisterHandlers();
		this.Gui.Locale.Initialize();
	}

	private void SetupLegacy() {
		this._legacy.Setup();
		this._legacy.OnConfirmed += this.Setup;
	}
}
