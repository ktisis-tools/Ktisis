using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Ktisis.Events.Attributes;

namespace Ktisis.Events;

public interface IEventClient {
	private const BindingFlags Binding = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	public EventInfo? GetEmitter(Type type) => GetEmitters()
		.FirstOrDefault(e => e?.EventHandlerType == type, null);

	public IEnumerable<EventInfo> GetEmitters() => GetType().GetEvents(Binding)
		.Where(e => e.GetCustomAttribute<EventEmitterAttribute>() is not null);

	public IEnumerable<MethodInfo> GetListeners() => GetType().GetMethods(Binding)
		.Where(m => m.GetCustomAttribute<ListenerAttribute>() is not null);
}
