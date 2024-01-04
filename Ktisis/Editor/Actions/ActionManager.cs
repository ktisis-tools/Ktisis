using System;
using System.Collections.Generic;

using Ktisis.Editor.Actions.Input;
using Ktisis.Editor.Actions.Types;
using Ktisis.Editor.Context;

namespace Ktisis.Editor.Actions;

public interface IActionManager {
	public IEditorContext Context { get; }
	
	public IInputManager Input { get; }
	public IHistoryManager History { get; }
	
	public bool IsValid { get; }

	public T Get<T>() where T : ActionBase;

	public void Register<T>(T inst) where T : ActionBase;
	public void Register<T>(Type type, T inst) where T : ActionBase;

	public void Initialize();
}

public class ActionManager : IActionManager, IDisposable {
	private readonly IContextMediator _mediator;
    
	private readonly Dictionary<Type, ActionBase> Actions = new();

	public IEditorContext Context => this._mediator.Context;

	public IInputManager Input { get; }
	public IHistoryManager History { get; }
	
	public bool IsValid => this.Context.IsValid;

	public ActionManager(
		IContextMediator mediator,
		IInputManager input
	) {
		this._mediator = mediator;
		this.Input = input;
		this.History = new HistoryManager();
	}
	
	// Actions

	public T Get<T>() where T : ActionBase
		=> (T)this.Actions[typeof(T)];

	public void Register<T>(T inst) where T : ActionBase
		=> this.Register(typeof(T), inst);
	
	public void Register<T>(Type type, T inst) where T : ActionBase {
		var attr = inst.GetAttribute();
		try {
			// TODO: if inst is IKeybind
			this.Actions.Add(type, inst);
			Ktisis.Log.Verbose($"Registered action: {attr.Name}");
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to register action '{attr.Name}':\n{err}");
		}
	}
	
	// Initialization

	public void Initialize() {
		
	}
	
	// Disposal
	
	public void Dispose() {
		try {
			this.History.Clear();
			foreach (var action in this.Actions.Values)
				if (action is IDisposable inst)
					inst.Dispose();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to dispose action manager:\n{err}");
		} finally {
			GC.SuppressFinalize(this);
		}
	}
}
