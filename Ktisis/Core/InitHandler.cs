using Ktisis.Core.Attributes;
using Ktisis.Data.Config;
using Ktisis.Editor;
using Ktisis.Editor.Context;
using Ktisis.Interface;
using Ktisis.Interop;
using Ktisis.Services;

namespace Ktisis.Core;

[Singleton]
public class InitHandler {
	private readonly ConfigManager _cfg;
	private readonly CommandService _cmd;
	private readonly DllResolver _dll;
	private readonly ContextManager _editor;
	private readonly GuiManager _gui;
	
	public InitHandler(
		ConfigManager cfg,
		CommandService cmd,
		DllResolver dll,
		ContextManager editor,
		GuiManager gui
	) {
		this._cfg = cfg;
		this._cmd = cmd;
		this._dll = dll;
		this._editor = editor;
		this._gui = gui;
	}

	public void Initialize() {
		this._dll.Create();
		this._cfg.Load();
		this._gui.Initialize();
		this._cmd.RegisterHandlers();
		this._editor.Initialize();
	}
}
