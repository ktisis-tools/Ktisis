using System;

using Ktisis.Actions.Types;
using Ktisis.Editor.Actions.Input;
using Ktisis.Editor.Context.Types;

namespace Ktisis.Editor.Actions;

public interface IActionManager {
	public IInputManager Input { get; }
	public IHistoryManager History { get; }

	public void Initialize();
}

public class ActionManager : IActionManager, IDisposable {
	private readonly IEditorContext _ctx;

	public IInputManager Input { get; }
	public IHistoryManager History { get; }

	public ActionManager(
		IEditorContext ctx,
		IInputManager input
	) {
		this._ctx = ctx;
		this.Input = input;
		this.History = new HistoryManager();
	}
	
	// Initialization

	public void Initialize() {
		Ktisis.Log.Verbose("Initializing input manager...");
		try {
			this.Input.Initialize();
			this.RegisterKeybinds();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to initialize input manager:\n{err}");
		}
	}

	private void RegisterKeybinds() {
		var actions = this._ctx.Plugin.Actions.GetBindable();
		foreach (var action in actions)
			this.RegisterKeybind(action);
	}

	private void RegisterKeybind(KeyAction action) {
		var keybind = action.GetKeybind();
		this.Input.Register(keybind, action.Invoke, action.BindInfo.Trigger);
	}
	
	// Disposal
	
	public void Dispose() {
		try {
			this.History.Clear();
			this.Input.Dispose();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to dispose action manager:\n{err}");
		} finally {
			GC.SuppressFinalize(this);
		}
	}
}
