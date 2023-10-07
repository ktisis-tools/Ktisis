using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Ktisis.Core;
using Ktisis.Events;
using Ktisis.Interface.Input;
using Ktisis.Interface.Input.Keys;
using Ktisis.Actions.Impl;

namespace Ktisis.Actions; 

[DIService]
public class ActionManager : IDisposable {
	private readonly IServiceContainer _services;
	private readonly InputManager _input;
	
	private readonly Dictionary<string, IAction> Actions = new();
	
	public ActionManager(
		IServiceContainer _services,
		InputManager _input,
		InitEvent _init
	) {
		this._services = _services;
		this._input = _input;
        
		_init.Subscribe(Initialize);
	}
	
	// Initialization
	
	private Dictionary<Type, ActionAttribute> ResolveActions() {
		return Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(type => type.GetInterfaces().Contains(typeof(IAction)))
			.Select(type => (type: type, attr: type.GetCustomAttribute<ActionAttribute>()))
			.Where(pair => pair.attr != null)
			.ToDictionary(k => k.type, v => v.attr)!;
	}

	private void Initialize() {
		Ktisis.Log.Verbose("Registering actions...");

		foreach (var (type, attr) in ResolveActions()) {
			try {
				RegisterAction(type, attr);
			} catch (Exception err) {
				Ktisis.Log.Error($"Failed to initialize action '{attr.Name}':\n{err}");
			}
		}
	}

	private void RegisterAction(Type type, ActionAttribute attr) {
		var inst = (IAction)this._services.GetService(type)!;
		if (inst is IKeybind)
			RegisterActionHotkey(attr.Name, inst);
		this.Actions.Add(attr.Name, inst);
	}

	private void RegisterActionHotkey(string name, IAction action) {
		var factory = new HotkeyFactory(name, action.Invoke);
		((IKeybind)action).BuildKeybind(factory);
		this._input.RegisterHotkey(factory.Create(), factory.GetDefaultKey());
	}
	
	// Action access
	
	public T Get<T>(string name) where T : IAction
		=> (T)this.Actions[name];

	public T Get<T>() where T : IAction
		=> (T)this.Actions.Values.First(action => action is T);
	
	// Disposal

	public void Dispose() {
		foreach (var action in this.Actions.Values) {
			if (action is IDisposable inst)
				Dispose(inst);
		}
		
		this.Actions.Clear();
	}

	private void Dispose(IDisposable inst) {
		try {
			inst.Dispose();
		} catch (Exception err) {
			Ktisis.Log.Error($"Failed to dispose {nameof(inst)}:\n{err}");
		}
	}
}
