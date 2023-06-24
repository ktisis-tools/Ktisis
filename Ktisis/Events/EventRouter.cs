using System;
using System.Collections.Generic;
using System.Reflection;

using Ktisis.Core.Singletons;
using Ktisis.Events.Attributes;
using Ktisis.Events.Providers;

namespace Ktisis.Events;

public class EventRouter : Service {
	// Internal event providers

	private readonly List<EventProvider> Providers = new();

	private void Create<T>() where T : EventProvider, new() {
		var provider = new T();
		Providers.Add(provider);
		Create(provider);
	}

	// Injection handling for emitters
	
	private readonly List<IEventClient> Emitters = new();

	private Dictionary<Type, List<Delegate>>? Queue;

	public void Create<T>(T client) where T : IEventClient {
		var events = client.GetEmitters();
		foreach (var @event in events) {
			var type = @event.EventHandlerType;
			if (type is null || Queue is null || !Queue.TryGetValue(type, out var queue))
				continue;
			
			queue.ForEach(d => @event.GetAddMethod(true)?.Invoke(client, new object?[] { d }));

			Queue.Remove(type);
			if (Queue.Count == 0)
				Queue = null;
		}

		Emitters.Add(client);

		var listeners = client.GetListeners();
		foreach (var listener in listeners) {
			var attr = listener.GetCustomAttribute<ListenerAttribute>();
			if (attr is null) continue;

			EventInfo? @event = null;
			IEventClient? emitter = null;
			foreach (var e in Emitters) {
				if (e.GetEmitter(attr.DelegateType) is not { } _event) continue;
				@event = _event;
				emitter = e;
				break;
			}

			var type = attr.DelegateType;
			var @delegate = Delegate.CreateDelegate(type, client, listener);
			if (emitter is not null && @event is not null) {
				@event.GetAddMethod(true)?.Invoke(emitter, new object?[] { @delegate });
			} else {
				if (Queue is not null && Queue.TryGetValue(type, out var queue)) {
					queue.Add(@delegate);
				} else {
					Queue ??= new();
					Queue.Add(type, new List<Delegate> { @delegate });
				}
			}
		}
	}

	public void Remove<T>(T client) where T : IEventClient {
		var listeners = client.GetListeners();
		foreach (var listener in listeners) {
			var attr = listener.GetCustomAttribute<ListenerAttribute>();
			if (attr is null) continue;

			EventInfo? @event = null;
			IEventClient? emitter = null;
			foreach (var e in Emitters) {
				if (e.GetEmitter(attr.DelegateType) is not { } _event) continue;
				@event = _event;
				emitter = e;
				break;
			}

			var type = attr.DelegateType;
			var @delegate = Delegate.CreateDelegate(type, client, listener);
			if (emitter is not null && @event is not null)
				@event.GetRemoveMethod(true)?.Invoke(emitter, new object?[] { @delegate });
		}
	}
	
	// Initialization handler

	public override void Init() {
		Create<FrameworkEventProvider>();
	}

	// Activate clients on plugin ready

	public override void OnReady() => Providers.ForEach(item => item.Setup());

	// Disposal

	public override void Dispose() {
		foreach (var provider in Providers) {
			provider.Dispose();
			Remove(provider);
		}
		Providers.Clear();
	}
}