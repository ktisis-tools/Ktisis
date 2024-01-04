using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Ktisis.Core;
using Ktisis.Core.Attributes;
using Ktisis.Editor.Actions.Input;
using Ktisis.Editor.Actions.Types;
using Ktisis.Editor.Context;
using Ktisis.Interop.Hooking;

namespace Ktisis.Editor.Actions;

[Singleton]
public class ActionBuilder {
	private readonly DIBuilder _di;
	
	public ActionBuilder(
		DIBuilder di
	) {
		this._di = di;
	}

	public ActionManager Initialize(
		IContextMediator mediator,
		HookScope scope
	) {
		var input = new InputManager(mediator, scope);
		
		var actions = new ActionManager(mediator, input);
		this.InitActions(actions);
		return actions;
	}

	private void InitActions(IActionManager actions) {
		foreach (var (type, attr) in this.ResolveActions()) {
			try {
				var inst = (ActionBase)this._di.Create(type, actions);
				actions.Register(type, inst);
			} catch (Exception err) {
				Ktisis.Log.Error($"Failed to create action '{attr.Name}'\n{err}");
			}
		}
	}
	
	private Dictionary<Type, ActionAttribute> ResolveActions() {
		return Assembly.GetExecutingAssembly()
			.GetTypes()
			.Select(type => (type: type, attr: type.GetCustomAttribute<ActionAttribute>()))
			.Where(pair => pair.attr != null)
			.ToDictionary(k => k.type, v => v.attr!);
	}
}
