using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Ktisis.Actions.Attributes;
using Ktisis.Core;
using Ktisis.Core.Types;
using Ktisis.Core.Attributes;
using Ktisis.Actions.Types;

namespace Ktisis.Actions;

[Singleton]
public class ActionService {
	private readonly DIBuilder _di;

	public ActionService(
		DIBuilder di
	) {
		this._di = di;
	}

	// Initialization
	
	private readonly Dictionary<Type, ActionBase> Actions = new();

	public void RegisterActions(IPluginContext context) {
		this.Actions.Clear();
		foreach (var (type, attr) in ResolveActions()) {
			try {
				var inst = (ActionBase)this._di.Create(type, context);
				this.Actions.Add(type, inst);
			} catch (Exception err) {
				Ktisis.Log.Error($"Failed to create action '{attr.Name}'\n{err}");
			}
		}
	}

	// Action access

	public T Get<T>() where T : ActionBase => (T)this.Actions[typeof(T)];

	public bool TryGet<T>(out T action) where T : ActionBase {
		T? result = null;
		if (this.Actions.TryGetValue(typeof(T), out var actionBase))
			result = (T)actionBase;
		action = result!;
		return result != null;
	}
	
	// Enumerators

	public IEnumerable<ActionBase> GetAll() => this.Actions.Values;

	public IEnumerable<KeyAction> GetBindable() => this.GetAll()
		.Where(action => action is KeyAction)
		.Cast<KeyAction>();
	
	// Type resolver
	
	private static Dictionary<Type, ActionAttribute> ResolveActions() {
		return Assembly.GetExecutingAssembly()
			.GetTypes()
			.Select(type => (type, attr: type.GetCustomAttribute<ActionAttribute>()))
			.Where(pair => pair.attr != null)
			.ToDictionary(k => k.type, v => v.attr!);
	}
}
