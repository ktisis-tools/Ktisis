using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Editor.Context;
using Ktisis.Interface;
using Ktisis.Interop;
using Ktisis.Legacy;
using Ktisis.Services;

namespace Ktisis.Core;

[Singleton]
public class SetupHandler {
	private readonly ConfigManager _cfg;
	private readonly CommandService _cmd;
	private readonly DllResolver _dll;
	private readonly ContextManager _editor;
	private readonly GuiManager _gui;
	private readonly LegacyMigrator _legacy;
	
	public SetupHandler(
		ConfigManager cfg,
		CommandService cmd,
		DllResolver dll,
		ContextManager editor,
		GuiManager gui,
		LegacyMigrator legacy
	) {
		this._cfg = cfg;
		this._cmd = cmd;
		this._dll = dll;
		this._editor = editor;
		this._gui = gui;
		this._legacy = legacy;
	}

	public void Initialize() {
		this._gui.Initialize();
		
		if (this._cfg.GetConfigFileExists())
			this.Setup();
		else
			this.SetupLegacy();
	}

	private void Setup() {
		this._dll.Create();
		this._cfg.Load();
		this._cmd.RegisterHandlers();
		this._editor.Initialize();
	}

	private void SetupLegacy() {
		this._legacy.Setup();
		this._legacy.OnConfirmed += this.Setup;
	}
}
