using System;

namespace Ktisis.Core.IoC; 

public interface IEventClient {
	public void Unsubscribe();
}

public sealed class EventClient<D> : IEventClient where D : Delegate {
	private readonly IEvent<D> _event;
	private readonly D _delegate;

	public EventClient(IEvent<D> _event, D _delegate) {
		this._event = _event;
		this._delegate = _delegate;
	}
	
	public void Unsubscribe() => this._event.Unsubscribe(this._delegate);
}
