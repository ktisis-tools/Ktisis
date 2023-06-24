using System;

namespace Ktisis.Events.Attributes;

public enum EventCondition {
	Default
}

[AttributeUsage(AttributeTargets.Event)]
public class EventEmitterAttribute : Attribute {
	public readonly EventCondition Condition;

	public EventEmitterAttribute(EventCondition condition = EventCondition.Default)
		=> Condition = condition;
}