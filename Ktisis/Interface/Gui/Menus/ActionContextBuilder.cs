using System;

using Ktisis.Core;
using Ktisis.Actions;
using Ktisis.Actions.Impl;
using Ktisis.Interface.Input;
using Ktisis.Interface.Localization;

namespace Ktisis.Interface.Gui.Menus; 

[DIService]
public class ActionContextBuilder {
	private readonly ActionManager _actions;
	private readonly InputManager _input;
	private readonly LocaleManager _locale;
	
	public ActionContextBuilder(ActionManager _action, InputManager _input, LocaleManager _locale) {
		this._actions = _action;
		this._input = _input;
		this._locale = _locale;
	}

	public ContextMenuFactory GetFactory(string name)
		=> new(name);

	public ActionContext GetActionContext<T>() where T : IAction {
		var action = this._actions.Get<T>();
		var name = action.GetName();

		var shortcut = string.Empty;
		if (this._input.TryGetHotkey(name, out var hotkey) && hotkey != null)
			shortcut = hotkey.Keybind.GetShortcutString();
        
		return new ActionContext {
			Name = this._locale.Translate(name),
			Shortcut = shortcut,
			Invoke = () => {
				if (action.CanInvoke())
					action.Invoke();
			}
		};
	}
	
	public ActionContextBuilder AddActionContext<T>(ContextMenuFactory factory) where T : IAction {
		var context = GetActionContext<T>();
		context.AddTo(factory);
		return this;
	}

	public ActionContextBuilder AddActionContext<T>(ContextNodeFactory factory) where T : IAction {
		var context = GetActionContext<T>();
		context.AddTo(factory);
		return this;
	}

	public sealed class ActionContext {
		public required string Name;
		public required string Shortcut;
		public required Action Invoke;

		public void AddTo<T>(IContextNodeFactoryBase<T> factory) {
			factory.AddAction(this.Name, this.Invoke, this.Shortcut);
		}
	}
}
