using System;
using System.Linq;
using System.Collections.Generic;

using JetBrains.Annotations;

using Ktisis.Events.Attributes;
using Ktisis.Common.Extensions;
using Ktisis.Core.Singletons;

namespace Ktisis.Events;

public delegate void ConditionEvent(Condition cond, bool value);

public class ConditionService : Service, IEventClient {
	// Internal
	
	private readonly static Condition[] ConditionTypes = Enum.GetValues<Condition>();
	private static Dictionary<Condition, bool> GenerateDict() {
		var result = new Dictionary<Condition, bool>();
		foreach (var cond in ConditionTypes)
			result[cond] = cond is Condition.Any;
		return result;
	}

	// Conditions
	
	private readonly Dictionary<Condition, bool> Conditions = GenerateDict();

	public bool Check(Condition flags) => ConditionTypes
		.Where(v => flags.HasFlag(v))
		.All(v => this[v]);

	public bool this[Condition cond] {
		get => Conditions[cond];
		set {
			Conditions[cond] = value;
			ConditionEvent?.InvokeSafely(cond, value);
		}
	}
	
	// Event emitter

	[EventEmitter, UsedImplicitly]
	public event ConditionEvent? ConditionEvent;
}

// Condition enum

[Flags]
public enum Condition {
	Any = 0,
	None = 1,
	IsInGPose = 2
}