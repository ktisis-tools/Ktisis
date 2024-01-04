using System;
using System.Collections.Generic;

namespace Ktisis.Events;

public abstract class EventBase<T> : IDisposable where T : Delegate {
	protected readonly HashSet<object> _subscribers = new();

	public bool Add(T subscriber) {
		lock (this._subscribers) {
			return this._subscribers.Add(subscriber);
		}
	}

	public bool Remove(T subscriber) {
		lock (this._subscribers) {
			return this._subscribers.Remove(subscriber);
		}
	}

	public void Dispose() {
		this._subscribers.Clear();
	}
}
