using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.Game;

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

using Lumina.Extensions;

namespace Ktisis.Core;

[Singleton]
public class PluginContext : IPluginContext {
	private readonly CommandService _cmd;
	private readonly DllResolver _dll;
	private readonly ContextManager _context;
	private readonly LegacyMigrator _legacy;
	private readonly IDalamudPluginInterface _dpi;
	private readonly IFramework _framework;
	
	public ActionService Actions { get; }
	public ConfigManager Config { get; }
	public GuiManager Gui { get; }
	public IpcManager Ipc { get; }

	public IEditorContext? Editor => this._context.Current;
	
	public PluginContext(
		ActionService actions,
		ConfigManager cfg,
		CommandService cmd,
		DllResolver dll,
		ContextManager context,
		GuiManager gui,
		IpcManager ipc,
		LegacyMigrator legacy,
		IDalamudPluginInterface dpi,
		IFramework framework
	) {
		this._cmd = cmd;
		this._dll = dll;
		this._context = context;
		this._legacy = legacy;
		this._dpi = dpi;
		this._framework = framework;

		this.Actions = actions;
		this.Config = cfg;
		this.Gui = gui;
		this.Ipc = ipc;
	}

	public void Initialize() {
		if(this._dpi.IsTesting)
			this.RemoveTesting();
			
		if (this.Config.GetConfigFileExists()) {
			this.Config.Load();
			if (this.Config.File.Version < 12)
				this.SetupLegacy();
			else
				this.Setup();
		} else {
			if (this._dpi.GetPluginConfig() != null)
				this.SetupLegacy();
			else
				this.Setup();
		}
		this.Gui.Initialize();
		if (GameMain.IsInGPose()) {
			this._framework.RunOnFrameworkThread(() => {
				this._context.SetupEditor();
				Ktisis.Log.Verbose("Setup onload");
			});
			this._context.Current?.Interface.ToggleWorkspaceWindow();
		}
	}

	private void RemoveTesting() {
		var temp = Assembly.GetAssembly(typeof(IDalamudPluginInterface)).DefinedTypes.First(t => t.Name == "DalamudConfiguration").AsType(); 
		dynamic config =  this._dpi.GetService(temp);
		var list = (System.Collections.IList)temp.GetProperty("PluginTestingOptIns")!.GetValue(config)!;
		int index = -1;
		foreach (var entry in list) {
			string comp = (string)entry.GetType().GetProperty("InternalName")?.GetValue(entry);
			if(comp == "Ktisis")
				index = list.IndexOf(entry);
		}
		if(index != -1)
			list.RemoveAt(index);
	}

	private void Setup() {
		if (!this.Config._isLoaded)
			this.Config.Load();
		this.Gui.AddSettings();
		this._dll.Create();
		this.Actions.RegisterActions(this);
		this._context.Initialize(this);
		this._cmd.RegisterHandlers();
		this.Gui.Locale.Initialize(this.Config);
	}

	private void SetupLegacy() {
		this._legacy.Setup();
		this._cmd.RegisterLegacy();
		this._legacy.OnConfirmed += this.Setup;
	}
}
