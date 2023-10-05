using System;
using System.Collections.Generic;

namespace Ktisis.Core.IoC; 

public abstract class EventBase<D> : IDisposable, IEvent<D> where D : Delegate {
	private readonly HashSet<object> _subscribers = new();

	// Subscribers
	
	public IEventClient Subscribe(D handler) {
		this._subscribers.Add(handler);
		return new EventClient<D>(this, handler);
	}

	public void Unsubscribe(D handler) => this._subscribers.Remove(handler);
	
	// Invocation
	
	public void Invoke() {
		foreach (var _event in this._subscribers)
			((Action)_event).Invoke();
	}
	
	public void Invoke<T1>(T1 _sender) {
		foreach (var _event in this._subscribers)
			((Action<T1>)_event).Invoke(_sender);
	}
	
	public void Invoke<T1, T2>(T1 _sender, T2 _args) {
		foreach (var _event in this._subscribers)
			((Action<T1, T2>)_event).Invoke(_sender, _args);
	}
	
	// Disposal
	
	public void Dispose() => this._subscribers.Clear();
}
