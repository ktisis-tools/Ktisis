using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Ktisis.Actions.Types;
using Ktisis.Core;
using Ktisis.Core.Attributes;
using Ktisis.Core.Types;

namespace Ktisis.Actions;

[Singleton]
public class ActionsService {
	private readonly DIBuilder _di;

	public ActionsService(
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

	public IEnumerable<ActionBase> GetAll() => this.Actions.Values;
	
	// Type resolver
	
	private static Dictionary<Type, ActionAttribute> ResolveActions() {
		return Assembly.GetExecutingAssembly()
			.GetTypes()
			.Select(type => (type, attr: type.GetCustomAttribute<ActionAttribute>()))
			.Where(pair => pair.attr != null)
			.ToDictionary(k => k.type, v => v.attr!);
	}
}
